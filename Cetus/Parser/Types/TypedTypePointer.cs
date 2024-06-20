using Cetus.Parser.Tokens;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypePointer : TypedTypeWithPattern
{
	public TypedTypePointer(TypedType innerType)
	{
		InnerType = innerType;
	}
	
	public TypedTypePointer() { }
	
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(InnerType.LLVMType, 0);
	public string Name => "Pointer";
	public IToken Pattern => new TokenString([new LiteralToken("&"), new ParameterTypeToken("innerType")]);
	public float Priority => 10;
	public TypeParameters TypeParameters => new(["innerType"]);
	public TypedType InnerType { get; }
	public override string ToString() => $"{Name}[{InnerType}]";
	
	public TypedType Call(IHasIdentifiers context, TypeArgs args)
	{
		return new TypedTypePointer(args["innerType"].Type);
	}
}