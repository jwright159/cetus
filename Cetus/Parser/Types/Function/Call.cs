using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Call() : TypedTypeFunctionSimple("Call", Visitor.AnyValueType, [(Visitor.AnyFunctionType, "function"), (Visitor.AnyValueType.List(), "arguments")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		TypedValue function = args["function"];
		List<TypedValue> arguments = ((TypedValueCompiler<List<TypedValue>>)args["arguments"]).CompilerValue;
		TypedTypeFunction functionType;
		if (function.Type is TypedTypeClosurePointer closurePtr)
		{
			functionType = closurePtr.BlockType;
			
			LLVMValueRef functionPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.LLVMValue, 0, "functionPtrPtr");
			LLVMValueRef functionPtr = builder.BuildLoad2(functionType.Pointer().LLVMType, functionPtrPtr, "functionPtr");
			LLVMValueRef environmentPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.LLVMValue, 1, "environmentPtrPtr");
			LLVMValueRef environmentPtr = builder.BuildLoad2(Visitor.CharType.Pointer().LLVMType, environmentPtrPtr, "environmentPtr");
			
			function = new TypedValueValue(functionType, functionPtr);
			arguments.Insert(0, new TypedValueValue(Visitor.CharType.Pointer(), environmentPtr));
		}
		else if (function.Type is TypedTypePointer functionPointer)
		{
			functionType = (TypedTypeFunction)functionPointer.InnerType;
			function = new TypedValueValue(functionType, builder.BuildLoad2(functionType.LLVMType, function.LLVMValue, "functionValue"));
		}
		else
			functionType = (TypedTypeFunction)function.Type;
		
		TypedType? varArgType = functionType.Parameters.VarArg?.Type.Type;
		arguments.AddRange(functionCall.Arguments
			.Enumerate((paramIndex, arg) => VisitExpression(program, arg, paramIndex < functionType.NumParams ? functionType.Parameters[paramIndex].Type : varArgType)));
		
		if (functionType.IsVarArg ? arguments.Count < functionType.Parameters.Count : arguments.Count != functionType.NumParams)
			throw new Exception($"Argument count mismatch in call to '{functionType.Name}', expected {(functionType.IsVarArg ? "at least " : "")}{functionType.Parameters.Count} but got {arguments.Count}");
		
		foreach ((TypedValue argument, (TypedType type, string _)) in arguments.Zip(functionType.Parameters))
			if (!argument.IsOfType(type))
				throw new Exception($"Argument type mismatch in call to '{functionType.Name}', expected {type} but got {argument.Type.LLVMType}");
		
		TypedValue result = functionType.Call(context, arguments);
		
		if (typeHint is not null)
			result = result.CoersePointer(typeHint, builder, functionType.Name);
		
		return result;
	}
}