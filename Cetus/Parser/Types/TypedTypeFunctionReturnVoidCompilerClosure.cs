using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnVoidCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("ReturnVoid", Parser.VoidType, [], null, "return")
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		closure.ReturnValue = null;
		return Parser.Void;
	}
}