using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class FunctionCallContext(TypedTypeFunction fuctionType, FunctionArgs args) : TypedValue
{
	public TypedTypeFunction FunctionType => fuctionType;
	public TypedType Type => FunctionType.ReturnType.Type;
	public LLVMValueRef LLVMValue { get; private set; }
	public FunctionArgs Arguments => args;
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
	
	public TypedValue Call(IHasIdentifiers context)
	{
		return FunctionType.Call(context, Arguments);
	}
}

public partial class Parser
{
	public Result ParseFunctionCall(IHasIdentifiers program, out FunctionCallContext functionCall, int order = 0)
	{
		int startIndex = lexer.Index;
		
		foreach (TypedTypeFunction function in program.GetFinalizedFunctions().Skip(order))
		{
			order++;
			IToken token = function.Pattern!;
			FunctionArgs arguments = new(function.Parameters);
			
			if (!ParseToken(program, token, arguments, order))
			{
				lexer.Index = startIndex;
				continue;
			}
			
			functionCall = new FunctionCallContext(function, arguments);
			
			return new Result.Ok();
		}
		
		lexer.Index = startIndex;
		functionCall = null;
		return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
	}
	
	public bool ParseToken(IHasIdentifiers context, IToken token, FunctionArgs arguments, int order)
	{
		if (token is ParameterExpressionToken expressionToken)
		{
			if (ParseExpression(context, out TypedValue expression, order) is Result.Failure)
			{
				return false;
			}
			arguments[expressionToken.ParameterName] = expression;
			return true;
		}
		
		if (token is ParameterValueToken valueToken)
		{
			if (!lexer.Eat(out Word? value))
			{
				return false;
			}
			arguments[valueToken.ParameterName] = new ValueIdentifierContext(value.Value);
			return true;
		}
		
		return lexer.Eat(token);
	}
}