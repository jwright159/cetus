using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class Double : TypedValue, IToken
{
	public double Value { get; private set; }
	public TypedType Type => Visitor.DoubleType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMValue = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, Value);
	}
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		if (char.IsDigit(lexer.Current))
		{
			bool dot = false;
			for (lexer.Index++; lexer.Index < lexer.Length && (char.IsDigit(lexer.Current) || lexer.Current == '.'); lexer.Index++)
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
			
			if (!dot)
				return new Result.TokenRuleFailed($"Expected decimal point, got {lexer.Current}", lexer, startIndex);
			
			Value = double.Parse(lexer[startIndex..lexer.Index]);
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected digit, got {lexer.Current}", lexer, startIndex);
	}
}