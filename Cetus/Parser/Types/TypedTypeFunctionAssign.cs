using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionAssign() : TypedTypeFunction("Assign", Parser.VoidType, [Parser.IntType.Pointer(), Parser.IntType], null, null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		builder.BuildStore(args[1].Value, args[0].Value);
		return Parser.Void;
	}
}