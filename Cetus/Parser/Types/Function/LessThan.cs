using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class LessThan() : TypedTypeFunction("LessThan", Visitor.IntType, [(Visitor.IntType, "a"), (Visitor.IntType, "b")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef lessThan = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args[0].Value, args[1].Value, "lttmp");
		return new TypedValueValue(Visitor.BoolType, lessThan);
	}
}