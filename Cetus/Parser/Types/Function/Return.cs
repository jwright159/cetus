using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Return() : TypedTypeFunction("Return", Visitor.VoidType, [(Visitor.IntType, "value")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		if (args[0] is null)
			builder.BuildRetVoid();
		else
			builder.BuildRet(args[0].Value);
		return Visitor.Void;
	}
}