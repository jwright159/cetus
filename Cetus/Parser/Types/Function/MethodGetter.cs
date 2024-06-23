using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodGetter(TypedType @struct, TypedTypeFunction returnFunction) : TypedTypeFunctionBase
{
	public override string Name => $"{@struct.Name}.{returnFunction.Name}";
	public override IToken? Pattern => null;
	public override TypeIdentifier ReturnType => returnFunction.Id();
	public override FunctionParameters Parameters => new([(Visitor.TypeType, "type")], null);
	public override float Priority => 0;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new TypedValueType(new Lambda(Name, returnFunction, args.Keys[0], args["type"]));
	}
	
	private class Lambda(string methodName, TypedTypeFunction calledFunction, string argName, TypedValue arg) : TypedTypeFunctionSimple, ITypedTypeRequiresVisit
	{
		public override string Name => $"{methodName}_Lambda";
		public override IToken? Pattern => null;
		public override TypeIdentifier ReturnType => calledFunction.ReturnType;
		public override FunctionParameters Parameters => parameters;
		public FunctionParameters? parameters = null;
		public override float Priority => 0;
		
		private bool isCall;
		
		public void Visit(IHasIdentifiers context, Visitor visitor)
		{
			arg.Visit(context, null, visitor);
			TypedValue value = ((ValueIdentifier)arg).Value; // This won't work for expressions, deal with that later
			if (value is TypedValueType)
			{
				isCall = false;
				parameters = calledFunction.Parameters;
			}
			else if (value is TypedValueValue)
			{
				isCall = true;
				parameters = new FunctionParameters(calledFunction.Parameters.Parameters.Skip(1), calledFunction.Parameters.VarArg);
			}
			else
				throw new Exception("Invalid method call type");
		}
		
		public override LLVMValueRef? VisitResult(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
		{
			if (isCall)
			{
				args = new FunctionArgs(calledFunction.Parameters, args);
				args[args.Keys.First()] = arg;
			}
			
			TypedValue returnValue = calledFunction.Call(context, args);
			returnValue.Parse(context);
			returnValue.Transform(context, typeHint);
			returnValue.Visit(context, typeHint, visitor);
			
			return returnValue.LLVMValue;
		}
	}
}