using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class FunctionCall(IHasIdentifiers parent, int order, float priorityThreshold) : IToken, TypedValue
{
	public TypedTypeFunction FunctionType { get; private set; }
	public TypedType Type => FunctionType.ReturnType.Type;
	public LLVMValueRef LLVMValue { get; private set; }
	public FunctionArgs Arguments { get; private set; }
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		foreach (TypedTypeFunction function in parent.GetFinalizedFunctions().Where(func => func.Priority <= priorityThreshold).Skip(order))
		{
			order++;
			IToken token = function.Pattern!;
			FunctionArgs arguments = new(function.Parameters);
			
			Result result = lexer.Eat(token.Contextualize(parent, arguments, order, priorityThreshold));
			if (result is not Result.Passable)
			{
				lexer.Index = startIndex;
				continue;
			}
			
			FunctionType = function;
			Arguments = arguments;
			
			return result;
		}
		
		return new Result.TokenRuleFailed("Expected function call", lexer, startIndex);
	}
	
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
	
	public override string ToString() => $"{FunctionType.Name}{Arguments}";
}