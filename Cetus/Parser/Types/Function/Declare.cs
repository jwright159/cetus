using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Declare : TypedTypeFunctionSimple
{
	public override string Name => "Declare";
	public override IToken Pattern => new TokenString([new ParameterTypeToken("type"), new ParameterValueToken("name")]);
	public override TypeIdentifier ReturnType => Visitor.VoidType.Id();
	public override FunctionParameters Parameters => new([(Visitor.TypeType, "type"), (Visitor.CompilerStringType, "name")], null);
	public override float Priority => 99;
	
	public override LLVMValueRef? VisitResult(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		TypedType type = args["type"].Type;
		string name = ((ValueIdentifier)args["name"]).Name;
		LLVMValueRef variable = visitor.Builder.BuildAlloca(type.LLVMType, name);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		context.Identifiers.Add(name, result);
		return variable;
	}
}