using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionDeclare() : TypedTypeFunction("Declare", Parser.VoidType, [Parser.TypeType, Parser.CompilerStringType, Parser.IntType], null, [new ParameterIndexToken(0), new ParameterIndexToken(1), new Assign(), new ParameterIndexToken(2)])
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		TypedType type = args[0].Type;
		string name = ((TypedValueCompilerString)args[1]).StringValue;
		TypedValue value = args[2];
		if (!value.IsOfType(type))
			throw new Exception($"Type mismatch in assignment to '{name}', expected {type.LLVMType} but got {value.Type.LLVMType}");
		LLVMValueRef variable = builder.BuildAlloca(type.LLVMType, name);
		builder.BuildStore(value.Value, variable);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return Parser.Void;
	}
}