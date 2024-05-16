using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionAssign() : TypedTypeFunction("Assign", Visitor.VoidType, [Visitor.IntType.Pointer(), Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		builder.BuildStore(args[1].Value, args[0].Value);
		return Visitor.Void;
	}
}