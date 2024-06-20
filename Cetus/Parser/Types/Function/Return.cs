using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Return : TypedTypeFunctionSimple
{
	public override string Name => "Return";
	public override IToken Pattern => new TokenString([new LiteralToken("Return"), new TokenOptional(new ParameterExpressionToken("value"))]);
	public override TypeIdentifier ReturnType => Visitor.VoidType.Id();
	public override FunctionParameters Parameters => new([(Visitor.AnyValueType, "value")], null);
	public override float Priority => 100;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		IHasIdentifiers function = context;
		while (function is not DefineFunctionCall)
			function = function.Base;
		TypedType returnType = ((DefineFunctionCall)function).ReturnType.Type; // why are we expecting a pointer???
		
		TypedValue? result = args["value"];
		if (result is not null)
		{
			result.Visit(context, returnType, visitor);
			return visitor.Builder.BuildRet(result.LLVMValue);
		}
		return visitor.Builder.BuildRetVoid();
	}
}