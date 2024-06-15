using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class Float : TypedValue, IToken
{
	public float Value { get; private set; }
	public TypedType Type => Visitor.FloatType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMValue = LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, Value);
	}
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		if (char.IsDigit(lexer.Current))
		{
			bool dot = false;
			for (lexer.Index++; !lexer.IsAtEnd && (char.IsDigit(lexer.Current) || lexer.Current == '.'); lexer.Index++)
			{
				if (lexer.Current == '.')
				{
					if (dot)
					{
						lexer.Index--;
						break;
					}
					else
						dot = true;
				}
			}
			
			if (lexer.Current == 'f')
				lexer.Index++;
			else
				return new Result.TokenRuleFailed($"Expected 'f', got {lexer.Current}", lexer, startIndex);
			
			Value = float.Parse(lexer[startIndex..(lexer.Index - 1)]);
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected digit, got {lexer.Current}", lexer, startIndex);
	}
}