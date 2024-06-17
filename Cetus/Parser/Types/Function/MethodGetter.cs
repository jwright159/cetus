using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;

namespace Cetus.Parser.Types.Function;

public class MethodGetter(TypedType @struct, TypedTypeFunction returnFunction) : TypedTypeFunctionBase
{
	public override string Name => $"{@struct.Name}.Get_{returnFunction.Name}";
	public override IToken? Pattern => null;
	public override TypeIdentifier ReturnType => new(returnFunction);
	public override FunctionParameters Parameters => new([(Visitor.TypeType, "type")], null);
	public override float Priority => 0;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new TypedValueType(returnFunction);
	}
}