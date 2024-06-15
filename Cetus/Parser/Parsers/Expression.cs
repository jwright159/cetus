using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class Expression : TypedValue
{
	public LLVMBasicBlockRef Block;
	public TypedValue ReturnValue;
	
	public TypedType Type { get; }
	public LLVMValueRef LLVMValue { get; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMBasicBlockRef originalBlock = visitor.Builder.InsertBlock;
		LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
		visitor.Builder.PositionAtEnd(block);
		
		// Visit something here
		
		visitor.Builder.PositionAtEnd(originalBlock);
	}
}

public partial class Parser
{
	public Result ParseExpression(IHasIdentifiers context, out TypedValue expression, int order = 0)
	{
		if (ParseFunctionCall(context, out FunctionCallContext functionCall, order) is Result.Passable functionCallResult)
		{
			expression = functionCall;
			return Result.WrapPassable("Invalid expression", functionCallResult);
		}
		
		if (ParseValue(out TypedValue value) is Result.Passable valueResult)
		{
			expression = value;
			return Result.WrapPassable("Invalid expression", valueResult);
		}
		
		expression = null;
		return new Result.TokenRuleFailed("Expected expression", lexer.Line, lexer.Column);
	}
	
	public Result ParseValue(out TypedValue value)
	{
		if (lexer.Eat(out Tokens.HexInteger? hexInteger))
		{
			value = hexInteger;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.Float? @float))
		{
			value = @float;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.Double? @double))
		{
			value = @double;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.DecimalInteger? decimalInteger))
		{
			value = decimalInteger;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.String? @string))
		{
			value = @string;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.Closure? closure))
		{
			value = closure;
			return new Result.Ok();
		}
		
		if (lexer.Eat(out Tokens.Word? valueName))
		{
			value = new ValueIdentifierContext(valueName.Value);
			return new Result.Ok();
		}
		
		value = null;
		return new Result.TokenRuleFailed("Expected value", lexer.Line, lexer.Column);
	}
}