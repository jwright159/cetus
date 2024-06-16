using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class While : TypedTypeFunctionSimple
{
	public override string Name => "While";
	public override IToken Pattern => new TokenString([new LiteralToken("While"), new LiteralToken("("), new ParameterExpressionToken("condition"), new LiteralToken(")"), new ParameterExpressionToken("body")]);
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(new TypedTypeCompilerExpression(Visitor.BoolType), "condition"), (new TypedTypeCompilerClosure(Visitor.VoidType), "body")], null);
	public override float Priority => 100;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		Expression condition = (Expression)args["condition"];
		Closure body = (Closure)((Expression)args["body"]).ReturnValue;
		LLVMBasicBlockRef merge = visitor.Builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		visitor.Builder.BuildBr(condition.Block);
		
		visitor.Builder.PositionAtEnd(condition.Block);
		visitor.Builder.BuildCondBr(condition.ReturnValue.LLVMValue, body.Block, merge);
		
		visitor.Builder.PositionAtEnd(body.Block);
		visitor.Builder.BuildBr(condition.Block);
		
		visitor.Builder.PositionAtEnd(merge);
		return null;
	}
}