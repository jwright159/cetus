using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public class FunctionParameterContext(TypeIdentifierContext type, string name)
{
	public TypeIdentifierContext Type => type;
	public string Name => name;
}

public class FunctionParameter(TypedType type, string name)
{
	public TypedType Type => type;
	public string Name => name;
}

public partial class Parser
{
	public Result ParseFunctionParameter(out FunctionParameterContext? parameter)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext? type) is Result.Passable typeResult &&
		    lexer.Eat(out Word? name))
		{
			parameter = new FunctionParameterContext(type, name.TokenText);
			return Result.WrapPassable($"Invalid function parameter for '{name.TokenText}'", typeResult);
		}
		else
		{
			lexer.Index = startIndex;
			parameter = null;
			return new Result.TokenRuleFailed("Expected function parameter", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public FunctionParameter VisitFunctionParameter(ProgramContext program, FunctionParameterContext parameter)
	{
		return new FunctionParameter(VisitTypeIdentifier(program, parameter.Type), parameter.Name);
	}
}