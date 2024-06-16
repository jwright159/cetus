using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class DefinedFunctionCall(string name, TypedValue function, TypeIdentifier returnType, FunctionParameters parameters) : TypedTypeFunctionSimple
{
	public override string Name => name;
	public override IToken? Pattern => null;
	public override TypeIdentifier ReturnType => returnType;
	public override FunctionParameters Parameters => parameters;
	public override float Priority => 0;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildCall2(LLVMType, function.LLVMValue, parameters.Parameters.Select(param => args[param.Name].LLVMValue).ToArray(), ReturnType.Type is TypedTypeVoid ? "" : Name + "Call");
	}
}