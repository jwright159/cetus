using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodCaller(TypedType @struct, TypedTypeFunction calledFunction) : TypedTypeFunctionSimple
{
	public override string Name => $"{@struct.Name}.Call_{calledFunction.Name}";
	public override IToken? Pattern => null;
	public override TypeIdentifier ReturnType => new(new Lambda(calledFunction, null, null));
	public override FunctionParameters Parameters => new([(@struct, "value")], null);
	public override float Priority => 0;
	
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		Lambda lambda = new(calledFunction, args.Keys[0], args["value"]);
		return new TypedValueType(lambda).LLVMValue;
	}
	
	private class Lambda(TypedTypeFunction calledFunction, string argName, TypedValue arg) : TypedTypeFunctionSimple
	{
		public override string Name => $"{calledFunction.Name}_Lambda";
		public override IToken? Pattern => null;
		public override TypeIdentifier ReturnType => calledFunction.ReturnType;
		public override FunctionParameters Parameters => new(calledFunction.Parameters.TupleParams.Skip(1).ToArray(), null);
		public override float Priority => 0;
		
		public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
		{
			args[argName] = arg;
			return calledFunction.Call(context, args).LLVMValue;
		}
	}
}