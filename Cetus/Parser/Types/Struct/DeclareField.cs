using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Struct;

public class DeclareField : TypedTypeFunctionBase
{
	public override string Name => "Declare";
	public override IToken Pattern => new TokenString([new ParameterTypeToken("type"), new ParameterValueToken("name")]);
	public override TypeIdentifier ReturnType => Visitor.VoidType.Id();
	public override FunctionParameters Parameters => new([(Visitor.TypeType, "type"), (Visitor.CompilerStringType, "name")], null);
	public override float Priority => 70;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new StructField(
			(TypeIdentifier)args["type"],
			((ValueIdentifier)args["name"]).Name);
	}
}

public class StructField(TypeIdentifier type, string name) : TypedValue
{
	public string Name => name;
	public TypedType Type => TypeIdentifier.Type;
	public LLVMValueRef LLVMValue => throw new Exception("StructField does not have an LLVMValue");
	public TypeIdentifier TypeIdentifier => type;
	public Getter Getter;
	public uint Index { get; set; }
	
	public void Parse(IHasIdentifiers context)
	{
		TypeIdentifier.Parse(context);
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		TypeIdentifier.Transform(context, typeHint);
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		TypeIdentifier.Visit(context, null, visitor);
	}
	
	public override string ToString() => $"{Type} {Name}";
}