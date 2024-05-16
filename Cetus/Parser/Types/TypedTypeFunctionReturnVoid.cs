using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnVoid() : TypedTypeFunction("ReturnVoid", Visitor.VoidType, [], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		builder.BuildRetVoid();
		return Visitor.Void;
	}
}