from __future__ import annotations

from errors.blame import BlameSeverity
from processing.lexical.tokens.token_type import TokenType
from processing.syntactic.expressions.block_expr import BlockExpr, BlockType
from processing.syntactic.expressions.expr import Expr, child_property


class ConditionalExpr(Expr):
    """conditional_expr:
       'if' preglobal_expr block
       {'elif' preglobal_expr block}
       ['else' block];
    """

    @child_property
    def condition(self) -> Expr:
        pass

    @child_property
    def then_block(self) -> BlockExpr:
        pass

    @child_property
    def else_block(self) -> BlockExpr:
        pass

    def __init__(
            self,
            parent: Expr = None,
            condition: Expr = None,
            then_block: BlockExpr = None,
            else_block: BlockExpr = None
    ):
        super().__init__(parent)
        self.condition = condition
        self.then_block = then_block
        self.else_block = else_block

    def parse(self, else_if: bool = False) -> ConditionalExpr:
        if not else_if:
            self.stream.eat(TokenType.keyword_if)
        self.condition = self.parse_infix()
        self.then_block = BlockExpr(self).parse(BlockType.default)
        if self.stream.maybe_eat(TokenType.keyword_else):
            self.else_block = BlockExpr(self).parse(BlockType.default)
        elif self.stream.maybe_eat(TokenType.keyword_elif):
            self.else_block = BlockExpr(self).parse(BlockType.default)
        elif else_if:
            self.source.blame("'else' expected.", self.stream.peek, BlameSeverity.error)
        return self