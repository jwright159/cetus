using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionLessThan() : TypedTypeFunction("LessThan", Parser.IntType, [Parser.IntType, Parser.IntType], null, null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		LLVMValueRef lessThan = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args[0].Value, args[1].Value, "lttmp");
		LLVMValueRef lessThanExt = builder.BuildZExt(lessThan, LLVMTypeRef.Int32, "lttmpint");
		return new TypedValueValue(Parser.IntType, lessThanExt);
	}
}