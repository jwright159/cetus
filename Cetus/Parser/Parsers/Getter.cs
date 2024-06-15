using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class GetterContext(TypedType @struct, StructFieldContext field) : TypedTypeFunction
{
	public LLVMTypeRef LLVMType { get; }
	public string Name => $"{@struct.Name}.Get_{field.Name}";
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public TypedType Struct => @struct;
	public StructFieldContext Field => field;
	public IToken? Pattern { get; } = new TokenString([new ParameterExpressionToken(field.Name), new LiteralToken("."), new LiteralToken(field.Name)]);
	public TypeIdentifier ReturnType => field.TypeIdentifier;
	public float Priority => 0;
	public FunctionParameters Parameters { get; } = new()
	{
		Parameters = [new FunctionParameter(field.TypeIdentifier.Pointer(), field.Name)]
	};
	
	public TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		throw new NotImplementedException();
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}