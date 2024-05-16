using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IValueContext : IExpressionContext;

public partial class Parser
{
	public Result ParseValue(IHasIdentifiers program, out IValueContext value)
	{
		if (ParseHexInteger(out IntegerContext hexInteger) is Result.Passable hexIntegerResult)
		{
			value = hexInteger;
			return Result.WrapPassable("Invalid value", hexIntegerResult);
		}
		
		if (ParseFloat(out FloatContext @float) is Result.Passable floatResult)
		{
			value = @float;
			return Result.WrapPassable("Invalid value", floatResult);
		}
		
		if (ParseDouble(out DoubleContext @double) is Result.Passable doubleResult)
		{
			value = @double;
			return Result.WrapPassable("Invalid value", doubleResult);
		}
		
		if (ParseDecimalInteger(out IntegerContext decimalInteger) is Result.Passable decimalIntegerResult)
		{
			value = decimalInteger;
			return Result.WrapPassable("Invalid value", decimalIntegerResult);
		}
		
		if (ParseString(out StringContext @string) is Result.Passable stringResult)
		{
			value = @string;
			return Result.WrapPassable("Invalid value", stringResult);
		}
		
		if (ParseClosure(program, out ClosureContext closure) is Result.Passable closureResult)
		{
			value = closure;
			return Result.WrapPassable("Invalid value", closureResult);
		}
		
		if (ParseNull(out NullContext @null) is Result.Passable nullResult)
		{
			value = @null;
			return Result.WrapPassable("Invalid value", nullResult);
		}
		
		if (ParseValueIdentifier(out ValueIdentifierContext valueIdentifier) is Result.Passable valueIdentifierResult)
		{
			value = valueIdentifier;
			return Result.WrapPassable("Invalid value", valueIdentifierResult);
		}
		
		value = null;
		return new Result.TokenRuleFailed("Expected value", lexer.Line, lexer.Column);
	}
}

public partial class Visitor
{
	public TypedValue VisitValue(IHasIdentifiers program, IValueContext value, TypedType? typeHint)
	{
		if (value is IntegerContext integer)
			return VisitInteger(integer);
		if (value is FloatContext @float)
			return VisitFloat(@float);
		if (value is DoubleContext @double)
			return VisitDouble(@double);
		if (value is StringContext @string)
			return VisitString(@string, typeHint);
		if (value is ClosureContext closure)
			return VisitClosure(program, closure, typeHint);
		if (value is NullContext @null)
			return VisitNull(@null, typeHint);
		if (value is ValueIdentifierContext valueIdentifier)
			return VisitValueIdentifier(program, valueIdentifier, typeHint);
		throw new Exception("Unknown value type {value}");
	}
}