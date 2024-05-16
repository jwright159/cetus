using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionAdd() : TypedTypeFunction("Add", Visitor.IntType, [Visitor.IntType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef sum = builder.BuildAdd(args[0].Value, args[1].Value, "addtmp");
		return new TypedValueValue(Visitor.IntType, sum);
	}
}