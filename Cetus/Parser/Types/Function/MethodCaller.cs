using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodCaller(TypedType @struct, TypedTypeFunction calledFunction) : TypedTypeFunctionSimple($"{@struct.Name}.Call_{calledFunction.Name}", new Lambda(calledFunction, null, null), [(@struct, "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		Lambda lambda = new(calledFunction, args.Keys[0], args["value"]);
		return new TypedValueType(lambda).LLVMValue;
	}
	
	private class Lambda(TypedTypeFunction calledFunction, string argName, TypedValue arg) : TypedTypeFunctionSimple($"{calledFunction.Name}_Lambda", calledFunction.ReturnType.Type, calledFunction.Parameters.TupleParams.Skip(1).ToArray(), null)
	{
		public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
		{
			args[argName] = arg;
			return calledFunction.Call(context, args).LLVMValue;
		}
	}
}