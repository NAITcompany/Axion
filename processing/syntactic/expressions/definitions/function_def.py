from __future__ import annotations

from typing import List, Set, Optional
from xml.dom.minicompat import NodeList

from errors.blame import BlameType, BlameSeverity
from processing.codegen.code_builder import CodeBuilder
from processing.lexical.tokens.token import Token
from processing.lexical.tokens.token_type import TokenType
from processing.syntactic.expressions.atomic.name_expr import NameExpr
from processing.syntactic.expressions.block_expr import BlockExpr, BlockType
from processing.syntactic.expressions.expr import Expr, child_property
from processing.syntactic.expressions.expression_groups import DefinitionExpression, AtomExpression
from processing.syntactic.expressions.type_names import TypeName


class FunctionParameter(Expr):
    """parameter:
       ID ':' type ['=' infix_expr];
    """

    @child_property
    def name(self) -> NameExpr:
        pass

    @child_property
    def value_type(self) -> TypeName:
        pass

    @child_property
    def default_value(self) -> Expr:
        pass

    def __init__(
            self,
            parent: Expr,
            name: Expr = None,
            colon_token: Token = None,
            value_type: TypeName = None,
            equals_token: Token = None,
            default_value: Expr = None
    ):
        super().__init__(parent)
        self.name = name
        self.colon_token = colon_token
        self.value_type = value_type
        self.equals_token = equals_token
        self.default_value = default_value

    def parse(self, names: Set[str]) -> FunctionParameter:
        self.name = NameExpr(self).parse(must_be_simple = True)
        self.colon_token = self.stream.eat(TokenType.colon)
        self.value_type = TypeName(self).parse()
        if self.stream.maybe_eat(TokenType.op_assign):
            self.equals_token = self.stream.token
            self.default_value = self.parse_infix()

        if str(self.name) in names:
            self.source.blame(BlameType.duplicated_parameter_in_function, self)
        names.add(str(self.name))
        return self

    def to_axion(self, c: CodeBuilder):
        c += self.name
        if self.value_type is not None:
            c += self.colon_token, self.value_type
        if self.default_value is not None:
            c += self.equals_token, self.default_value

    def to_csharp(self, c: CodeBuilder):
        c += self.value_type, ' ', self.name
        if self.default_value is not None:
            c += ' = ', self.default_value


class FunctionDef(DefinitionExpression, AtomExpression):
    """func_def:
       'fn' name ['(' [parameters_list] ')'] ['=>' type] block;
    """

    @child_property
    def name(self) -> NameExpr:
        pass

    @child_property
    def return_type(self) -> TypeName:
        pass

    @child_property
    def parameters(self) -> NodeList:
        pass

    @child_property
    def block(self) -> BlockExpr:
        pass

    @child_property
    def modifiers(self) -> NodeList:
        pass

    @property
    def value_type(self) -> TypeName:
        raise NotImplementedError()

    def __init__(
            self,
            parent: Expr = None,
            fn_token: Token = None,
            name: Expr = None,
            parameters: List[Expr] = None,
            arrow_token: Token = None,
            return_type: Expr = None,
            block: BlockExpr = None,
            modifiers: List[Expr] = None
    ):
        super().__init__(parent)
        self.fn_token = fn_token
        self.name = name
        self.parameters = parameters
        self.arrow_token = arrow_token
        self.return_type = return_type
        self.block = block
        self.modifiers = modifiers

    def parse(self) -> FunctionDef:
        self.stream.eat(TokenType.keyword_fn)
        self.name = NameExpr(self).parse()
        if self.stream.maybe_eat(TokenType.open_parenthesis):
            self.parameters = FunctionDef.parse_param_list(self, TokenType.close_parenthesis)
            self.stream.eat(TokenType.close_parenthesis)
        if self.stream.maybe_eat(TokenType.right_fat_arrow):
            self.return_type = TypeName(self).parse()
        self.block = BlockExpr(self).parse(BlockType.default)
        return self

    @staticmethod
    def parse_param_list(parent: Expr, *terminators: TokenType) -> List[Expr]:
        """parameter_list:
           {named_parameter ","}
           ( "*" [parameter] ("," named_parameter)* ["," "**" parameter]
           | "**" parameter
           | named_parameter[","] )
        TODO rewrite syntax for fn parameters"""
        parameters: List[FunctionParameter] = []
        names: Set[str] = set()
        list_parameter: Optional[FunctionParameter] = None
        map_parameter: Optional[FunctionParameter] = None
        have_keyword_only_parameter = False
        got_star = False
        require_optionals = False
        if not parent.stream.peek.of_type(*terminators):
            while True:
                if parent.stream.maybe_eat(TokenType.op_power):
                    map_parameter = FunctionParameter(parent).parse(names)
                    parent.stream.eat(*terminators)
                    break
                elif parent.stream.maybe_eat(TokenType.op_multiply):
                    if list_parameter is not None:
                        parent.source.blame(
                            BlameType.cannot_have_more_than_1_list_parameter,
                            parent.stream.peek
                        )
                    if parent.stream.peek.of_type(TokenType.comma):
                        # '*,' - end of positional params mark.
                        got_star = True
                    else:
                        list_parameter = FunctionParameter(parent).parse(names)
                else:
                    if got_star:
                        param = FunctionParameter(parent).parse(names)
                        have_keyword_only_parameter = True
                    else:
                        param = FunctionParameter(parent).parse(names)
                    if param.default_value is not None:
                        require_optionals = True
                    elif require_optionals and param.default_value is None:
                        param.source.blame(
                            BlameType.expected_default_parameter_value,
                            param
                        )
                    parameters.append(param)
                if parent.stream.peek.of_type(*terminators):
                    break
                parent.stream.eat(TokenType.comma)
        if got_star \
                and list_parameter is None \
                and map_parameter is not None \
                and not have_keyword_only_parameter:
            parent.source.blame(
                "Named arguments must follow '*'",
                map_parameter,
                BlameSeverity.error
            )
        if list_parameter is not None:
            parameters.append(list_parameter)
        if map_parameter is not None:
            parameters.append(map_parameter)
        return parameters

    def to_axion(self, c: CodeBuilder):
        c += self.fn_token, self.name
        if len(self.parameters) > 0:
            c += '(', self.parameters, ')'
        c += self.arrow_token, self.return_type, self.block

    def to_csharp(self, c: CodeBuilder):
        c += 'public ', self.return_type, ' ', self.name, '(', self.parameters, ')', self.block