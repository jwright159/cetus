using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Program;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Return : TypedTypeFunctionSimple
{
	public override string Name => "Return";
	public override IToken Pattern => new TokenString([new LiteralToken("Return"), new TokenOptional(new ParameterExpressionToken("value"))]);
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(Visitor.IntType, "value")], null);
	public override float Priority => 100;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return args["value"] is null ? visitor.Builder.BuildRetVoid() : visitor.Builder.BuildRet(args["value"].LLVMValue);
	}
}