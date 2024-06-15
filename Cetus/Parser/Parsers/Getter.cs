using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class GetterContext(TypedType @struct, StructFieldContext field) : IFunctionContext
{
	public string Name => $"{@struct.Name}.Get_{field.Name}";
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public TypedType Struct => @struct;
	public StructFieldContext Field => field;
	public IToken? Pattern { get; } = new TokenString([new ParameterExpressionToken(field.Name), new LiteralToken("."), new LiteralToken(field.Name)]);
	public TypeIdentifier ReturnType => field.TypeIdentifier;
	public float Priority => 0;
	public FunctionParametersContext Parameters { get; } = new()
	{
		Parameters = [new FunctionParameterContext(field.TypeIdentifier.Pointer(), field.Name)]
	};
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}