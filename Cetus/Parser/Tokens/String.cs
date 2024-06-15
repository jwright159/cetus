using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class String : TypedValue, IToken
{
	public string Value { get; private set; }
	public TypedType Type => Visitor.StringType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMValue = visitor.Builder.BuildGlobalStringPtr(Value, Value.Length == 0 ? "emptyString" : Value);
	}
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		if (lexer.Current == '"')
		{
			for (lexer.Index++; !lexer.IsAtEnd && lexer.Current != '"'; lexer.Index++)
				if (lexer.Current == '\\')
					lexer.Index++;
			
			if (lexer.Current == '"')
				lexer.Index++;
			else
				return new Result.TokenRuleFailed("Expected '\"', got EOF", lexer, startIndex);
			
			Value = System.Text.RegularExpressions.Regex.Unescape(lexer[(startIndex + 1)..(lexer.Index - 1)]);
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected '\"', got {lexer.Current}", lexer, startIndex);
	}
}