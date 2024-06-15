using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Call() : TypedTypeFunctionSimple("Call", Visitor.AnyValueType, [(Visitor.AnyFunctionType, "function"), (Visitor.AnyValueType.List(), "arguments")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		TypedValue function = args["function"];
		List<TypedValue> arguments = ((TypedValueCompiler<List<TypedValue>>)args["arguments"]).CompilerValue;
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
		
		if (functionType.Parameters.VarArg is not null ? arguments.Count < functionType.Parameters.Count : arguments.Count != functionType.Parameters.Count)
			throw new Exception($"Argument count mismatch in call to '{functionType.Name}', expected {(functionType.Parameters.VarArg is not null ? "at least " : "")}{functionType.Parameters.Count} but got {arguments.Count}");
		
		foreach ((TypedType type, TypedValue argument) in functionType.Parameters
			         .ZipArgs(arguments, (param, arg) => (param.Type.Type, Arg: arg))
			         .Where(pair => pair.Arg.IsOfType(pair.Type)))
			throw new Exception($"Argument type mismatch in call to '{functionType.Name}', expected {type} but got {argument.Type.LLVMType}");
		
		FunctionArgs functionArgs = new(functionType.Parameters, arguments);
		TypedValue result = functionType.Call(context, functionArgs);
		
		if (typeHint is not null)
			result = result.CoersePointer(typeHint, visitor, functionType.Name);
		
		return result.LLVMValue;
	}
}