using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Declare : TypedTypeFunctionSimple
{
	public override string Name => "Declare";
	public override IToken Pattern => new TokenString([new LiteralToken("Declare"), new ParameterTypeToken("type"), new ParameterValueToken("name")]);
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(Visitor.TypeType, "type"), (Visitor.CompilerStringType, "name")], null);
	public override float Priority => 100;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		TypedType type = args["type"].Type;
		string name = ((TypedValueCompiler<string>)args["name"]).CompilerValue;
		LLVMValueRef variable = visitor.Builder.BuildAlloca(type.LLVMType, name);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return variable;
	}
}