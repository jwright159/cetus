using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionDeclare() : TypedTypeFunction("Declare", Parser.VoidType, [Parser.CompilerStringType, Parser.IntType], null, "Int $0 = $1")
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		TypedType type = Parser.IntType;
		string name = ((TypedValueCompilerString)args[0]).StringValue;
		TypedValue value = args[1];
		if (!TypedTypeExtensions.TypesEqual(type, value.Type))
			throw new Exception($"Type mismatch in assignment to '{name}', expected {type.LLVMType} but got {value.Type.LLVMType}");
		LLVMValueRef variable = builder.BuildAlloca(type.LLVMType, name);
		builder.BuildStore(value.Value, variable);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return Parser.Void;
	}
}