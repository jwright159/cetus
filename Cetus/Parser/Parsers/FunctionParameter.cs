using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public partial class Parser
{
	public class FunctionParameter(TypedType type, string name)
	{
		public TypedType Type => type;
		public string Name => name;
	}
	
	public Result ParseFunctionParameter(ProgramContext context, out FunctionParameter? parameter)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(context, out TypedType? type) is Result.Passable typeResult &&
		    lexer.Eat(out Word? name))
		{
			parameter = new FunctionParameter(type, name.TokenText);
			return Result.WrapPassable("Invalid function parameter", typeResult);
		}
		else
		{
			lexer.Index = startIndex;
			parameter = null;
			return new Result.TokenRuleFailed("Expected function parameter", lexer.Line, lexer.Column);
		}
	}
}