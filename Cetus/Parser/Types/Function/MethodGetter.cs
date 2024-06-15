using Cetus.Parser.Values;

namespace Cetus.Parser.Types.Function;

public class MethodGetter(TypedType @struct, TypedTypeFunction returnFunction) : TypedTypeFunction($"{@struct.Name}.Get_{returnFunction.Name}", returnFunction, [(Visitor.TypeType, "type")], null)
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new TypedValueType(returnFunction);
	}
}