using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Declare() : TypedTypeFunctionSimple("Declare", Visitor.VoidType, [(Visitor.TypeType, "type"), (Visitor.CompilerStringType, "name")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		TypedType type = args["type"].Type;
		string name = ((TypedValueCompiler<string>)args["name"]).CompilerValue;
		LLVMValueRef variable = visitor.Builder.BuildAlloca(type.LLVMType, name);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return variable;
	}
}