using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturn() : TypedTypeFunction("Return", Parser.VoidType, [Parser.IntType], null, null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		builder.BuildRet(args[0].Value);
		return Parser.Void;
	}
}