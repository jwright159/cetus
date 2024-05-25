using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Assign() : TypedTypeFunction("Assign", Visitor.VoidType, [(Visitor.IntType.Pointer(), "target"), (Visitor.IntType, "value")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		builder.BuildStore(args[1].Value, args[0].Value);
		return Visitor.Void;
	}
}