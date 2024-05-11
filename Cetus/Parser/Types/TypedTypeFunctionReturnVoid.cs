using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnVoid() : TypedTypeFunction("ReturnVoid", Parser.VoidType, [], null, null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		builder.BuildRetVoid();
		return Parser.Void;
	}
}