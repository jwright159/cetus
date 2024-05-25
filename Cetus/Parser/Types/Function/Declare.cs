using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Declare() : TypedTypeFunction("Declare", Visitor.VoidType, [(Visitor.TypeType, "type"), (Visitor.CompilerStringType, "name")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		TypedType type = args[0].Type;
		string name = ((TypedValueCompilerString)args[1]).StringValue;
		LLVMValueRef variable = builder.BuildAlloca(type.LLVMType, name);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return Visitor.Void;
	}
}