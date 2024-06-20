using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public interface IToken
{
	public Result Eat(Lexer lexer);
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) => this;
}