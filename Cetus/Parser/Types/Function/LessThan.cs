using Cetus.Parser.Tokens;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class LessThan : TypedTypeFunctionSimple
{
	public override string Name => "LessThan";
	public override IToken Pattern => new TokenString([new ParameterExpressionToken("a"), new LiteralToken("<"), new ParameterExpressionToken("b")]);
	public override TypeIdentifier ReturnType => Visitor.IntType.Id();
	public override FunctionParameters Parameters => new([(Visitor.IntType, "a"), (Visitor.IntType, "b")], null);
	public override float Priority => 40;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args["a"].LLVMValue, args["b"].LLVMValue, "lttmp");
	}
}