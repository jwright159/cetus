using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("Return", Visitor.VoidType, [Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		closure.ReturnValue = args[0];
		return Visitor.Void;
	}
}