using System.Globalization;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public abstract class Integer : TypedValue, IToken
{
	public int Value { get; protected set; }
	public TypedType Type => Visitor.IntType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMValue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Value, true);
	}
	
	public abstract Result Eat(Lexer lexer);
}

public class DecimalInteger : Integer
{
	public override Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		if (char.IsDigit(lexer.Current))
		{
			while (!lexer.IsAtEnd && char.IsDigit(lexer.Current)) lexer.Index++;
			Value = int.Parse(lexer[startIndex..lexer.Index]);
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected digit, got {lexer.Current}", lexer, startIndex);
	}
}

public class HexInteger : Integer
{
	public override Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		lexer.Index += 2;
		
		if (!lexer.IsAtEnd && lexer[startIndex..lexer.Index] == "0x")
		{
			while (!lexer.IsAtEnd && char.IsDigit(lexer.Current)) lexer.Index++;
			Value = int.Parse(lexer[(startIndex + 2)..lexer.Index], NumberStyles.HexNumber);
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected '0x', got {lexer.Contents[startIndex..(startIndex + 2)]}", lexer, startIndex);
	}
}