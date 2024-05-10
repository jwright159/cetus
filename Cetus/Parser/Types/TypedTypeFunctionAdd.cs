using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionAdd() : TypedTypeFunction("Add", Parser.IntType, [Parser.IntType, Parser.IntType], null, "$0 + $1")
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		LLVMValueRef sum = builder.BuildAdd(args[0].Value, args[1].Value, "addtmp");
		return new TypedValueValue(Parser.IntType, sum);
	}
}