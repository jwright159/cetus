using System.Text;

namespace CTes;

public class Lexer
{
	public static IEnumerable<Token> Lex(string input)
	{
		List<Token> tokens = [];
		StringReader reader = new(input);
		int line = 1;
		int column = 1;
		
		while (reader.Peek() != -1)
		{
			char c = (char)reader.Peek();
			
			if (char.IsWhiteSpace(c))
			{
				if (c == '\n')
				{
					line++;
					column = 1;
				}
				else
				{
					column++;
				}
				
				reader.Read();
				continue;
			}
			
			if (char.IsDigit(c))
			{
				StringBuilder number = new();
				number.Append(c);
				reader.Read();
				
				while (char.IsDigit((char)reader.Peek()))
				{
					number.Append((char)reader.Read());
				}
				
				tokens.Add(new Token(TokenType.Number, number.ToString(), line, column));
				column += number.Length;
				continue;
			}
			
			if (char.IsLetter(c))
			{
				StringBuilder identifier = new();
				identifier.Append(c);
				reader.Read();
				
				while (char.IsLetterOrDigit((char)reader.Peek()))
				{
					identifier.Append((char)reader.Read());
				}
				
				tokens.Add(new Token(TokenType.Identifier, identifier.ToString(), line, column));
				column += identifier.Length;
				continue;
			}
			
			switch (c)
			{
				case '+':
					tokens.Add(new Token(TokenType.Operator, "+", line, column));
					break;
				case '-':
					tokens.Add(new Token(TokenType.Operator, "-", line, column));
					break;
				case '*':
					tokens.Add(new Token(TokenType.Operator, "*", line, column));
					break;
				case '/':
					tokens.Add(new Token(TokenType.Operator, "/", line, column));
					break;
				case '(':
					tokens.Add(new Token(TokenType.LeftParenthesis, "(", line, column));
					break;
				case ')':
					tokens.Add(new Token(TokenType.RightParenthesis, ")", line, column));
					break;
				default:
					throw new Exception($"Unexpected character '{c}' at line {line}, column {column}");
			}
			
			reader.Read();
			column++;
		}
		
		return tokens;
	}
}

public class Token(TokenType type, string value, int line, int column)
{
	public TokenType Type { get; } = type;
	public string Value { get; } = value;
	public int Line { get; } = line;
	public int Column { get; } = column;
}

public enum TokenType
{
	Number,
	Identifier,
	Operator,
	LeftParenthesis,
	RightParenthesis
}