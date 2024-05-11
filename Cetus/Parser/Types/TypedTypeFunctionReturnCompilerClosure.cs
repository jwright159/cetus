using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionReturnCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("Return", Parser.VoidType, [Parser.IntType], null, null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		closure.ReturnValue = args[0];
		return Parser.Void;
	}
}