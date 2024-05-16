using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionLessThan() : TypedTypeFunction("LessThan", Visitor.IntType, [Visitor.IntType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef lessThan = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args[0].Value, args[1].Value, "lttmp");
		LLVMValueRef lessThanExt = builder.BuildZExt(lessThan, LLVMTypeRef.Int32, "lttmpint");
		return new TypedValueValue(Visitor.IntType, lessThanExt);
	}
}