using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class Expression(IHasIdentifiers parent, int order, float priorityThreshold) : IToken, TypedValue
{
	public LLVMBasicBlockRef Block;
	public TypedValue ReturnValue;
	
	public TypedType Type => ReturnValue.Type;
	public LLVMValueRef LLVMValue => ReturnValue.LLVMValue;
	
	public Result Eat(Lexer lexer)
	{
		FunctionCall functionCall = new(parent, order, priorityThreshold);
		if (lexer.Eat(functionCall) is Result.Passable functionCallResult)
		{
			ReturnValue = functionCall;
			return functionCallResult;
		}
		
		if (lexer.Eat(out HexInteger hexInteger) is Result.Passable hexIntegerResult)
		{
			ReturnValue = hexInteger;
			return hexIntegerResult;
		}
		
		if (lexer.Eat(out Float @float) is Result.Passable floatResult)
		{
			ReturnValue = @float;
			return floatResult;
		}
		
		if (lexer.Eat(out Double @double) is Result.Passable doubleResult)
		{
			ReturnValue = @double;
			return doubleResult;
		}
		
		if (lexer.Eat(out DecimalInteger decimalInteger) is Result.Passable decimalIntegerResult)
		{
			ReturnValue = decimalInteger;
			return decimalIntegerResult;
		}
		
		if (lexer.Eat(out String @string) is Result.Passable stringResult)
		{
			ReturnValue = @string;
			return stringResult;
		}
		
		if (lexer.Eat(out Closure closure) is Result.Passable closureResult)
		{
			ReturnValue = closure;
			return closureResult;
		}
		
		if (lexer.Eat(out ValueIdentifier value) is Result.Passable valueResult)
		{
			ReturnValue = value;
			return valueResult;
		}
		
		return new Result.TokenRuleFailed("Expected expression", lexer);
	}
	
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
		
		ReturnValue.Visit(context, typeHint, visitor);
		
		if (ReturnValue is FunctionCall call)
		{
			ReturnValue = call.Call(context);
			ReturnValue.Parse(context);
			ReturnValue.Transform(context, typeHint);
			ReturnValue.Visit(context, typeHint, visitor);
		}
		
		visitor.Builder.PositionAtEnd(originalBlock);
	}
	
	public override string ToString() => $"{ReturnValue}";
}