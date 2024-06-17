using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class CallFunction : TypedTypeFunctionBase
{
	public override string Name => "Call";
	public override IToken Pattern => new TokenString([new ParameterExpressionToken("function"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new ParameterExpressionToken("arguments", true))]);
	public override TypeIdentifier ReturnType => throw new Exception("Can't get return type of Call directly");
	public override FunctionParameters Parameters => new([(Visitor.AnyFunctionType, "function"), (Visitor.AnyValueType.List(), "arguments")], null);
	public override float Priority => 10;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new CallFunctionCall((visitContext, visitTypeHint, visitVisitor) => Visit(visitContext, visitTypeHint, visitVisitor, args));
	}
	
	public LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		args.Visit(context, visitor);
		TypedValue function = ((Expression)args["function"]).ReturnValue;
		List<TypedValue> arguments = []; 
		TypedTypeFunction functionType;
		if (function.Type is TypedTypeClosurePointer closurePtr)
		{
			functionType = closurePtr.BlockType;
			
			LLVMValueRef functionPtrPtr = visitor.Builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.LLVMValue, 0, "functionPtrPtr");
			LLVMValueRef functionPtr = visitor.Builder.BuildLoad2(functionType.Pointer().LLVMType, functionPtrPtr, "functionPtr");
			LLVMValueRef environmentPtrPtr = visitor.Builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.LLVMValue, 1, "environmentPtrPtr");
			LLVMValueRef environmentPtr = visitor.Builder.BuildLoad2(Visitor.CharType.Pointer().LLVMType, environmentPtrPtr, "environmentPtr");
			
			function = new TypedValueValue(functionType, functionPtr);
			arguments.Insert(0, new TypedValueValue(Visitor.CharType.Pointer(), environmentPtr));
		}
		else if (function.Type is TypedTypePointer functionPointer)
		{
			functionType = (TypedTypeFunction)functionPointer.InnerType;
			function = new TypedValueValue(functionType, visitor.Builder.BuildLoad2(functionType.LLVMType, function.LLVMValue, "functionValue"));
		}
		else
			functionType = (TypedTypeFunction)function.Type;
		
		if (args["arguments"] is TypedValueCompiler<List<TypedValue>> providedArgs)
			arguments.AddRange(providedArgs.CompilerValue);
		foreach ((FunctionParameter param, TypedValue arg) in functionType.Parameters
			         .ZipArgs(arguments, (param, arg) => (param, arg)))
			arg.Visit(context, param.Type.Type, visitor);
		
		if (functionType.Parameters.VarArg is not null ? arguments.Count < functionType.Parameters.Count : arguments.Count != functionType.Parameters.Count)
			throw new Exception($"Argument count mismatch in call to '{functionType.Name}', expected {(functionType.Parameters.VarArg is not null ? "at least " : "")}{functionType.Parameters.Count} but got {arguments.Count}");
		
		foreach ((TypedType type, TypedValue argument) in functionType.Parameters
			         .ZipArgs(arguments, (param, arg) => (param.Type.Type, Arg: arg))
			         .Where(pair => !pair.Arg.IsOfType(pair.Type)))
			throw new Exception($"Argument type mismatch in call to '{functionType.Name}', expected {type} but got {argument.Type.LLVMType}");
		
		FunctionArgs functionArgs = new(functionType.Parameters, arguments);
		TypedValue result = functionType.Call(context, functionArgs);
		result.Visit(context, null, visitor);
		
		if (typeHint is not null)
			result = result.CoersePointer(typeHint, visitor, functionType.Name);
		
		return result.LLVMValue;
	}
	
	private class CallFunctionCall(Func<IHasIdentifiers, TypedType?, Visitor, LLVMValueRef> visit) : TypedValue
	{
		public TypedType Type { get; private set; }
		public LLVMValueRef LLVMValue { get; private set; }
		
		public void Parse(IHasIdentifiers context)
		{
			
		}
		
		public void Transform(IHasIdentifiers context, TypedType? typeHint)
		{
			
		}
		
		public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
		{
			Type = typeHint;
			LLVMValue = visit(context, typeHint, visitor);
		}
	}
}