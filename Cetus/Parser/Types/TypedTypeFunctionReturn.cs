using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturn() : TypedTypeFunction("Return", Visitor.VoidType, [Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		builder.BuildRet(args[0].Value);
		return Visitor.Void;
	}
}