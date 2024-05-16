using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnVoidCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("ReturnVoid", Visitor.VoidType, [], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		closure.ReturnValue = null;
		return Visitor.Void;
	}
}