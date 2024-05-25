using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class GetterContext(StructDefinitionContext @struct, StructFieldContext field) : IFunctionContext
{
	public string Name => $"{@struct.Name}.Get_{field.Name}";
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public StructDefinitionContext Struct => @struct;
	public StructFieldContext Field => field;
	public IToken[]? Pattern { get; } = [new ParameterExpressionToken(0), new LiteralToken("."), new LiteralToken(field.Name)];
	public TypeIdentifierContext ReturnType => field.TypeIdentifier;
	public float Priority => 0;
	public FunctionParametersContext ParameterContexts { get; } = new()
	{
		Parameters = [new FunctionParameterContext(field.TypeIdentifier.Pointer(), field.Name)]
	};
	
	public override string ToString() => $"{ReturnType} {Name}{ParameterContexts}";
}

public partial class Visitor
{
	public void VisitGetter(GetterContext getter)
	{
		getter.Type = new Getter(getter);
		getter.Value = new TypedValueType(getter.Type);
	}
}