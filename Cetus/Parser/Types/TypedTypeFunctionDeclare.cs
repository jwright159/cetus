using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionDeclare() : TypedTypeFunction("Declare", Visitor.VoidType, [Visitor.TypeType, Visitor.CompilerStringType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
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
		return Visitor.Void;
	}
}