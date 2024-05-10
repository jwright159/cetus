using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public partial class Parser
{
	public class FunctionParameters(List<FunctionParameter> parameters, FunctionParameter? varArg)
	{
		public List<FunctionParameter> Parameters => parameters;
		public FunctionParameter? VarArg => varArg;
		
		public IEnumerable<TypedType> ParamTypes => parameters.Select(p => p.Type);
	}
	
	public Result ParseFunctionParameters(ProgramContext context, out FunctionParameters? parameters)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			List<Result> results = [];
			List<FunctionParameter> parametersList = [];
			
			while (ParseFunctionParameter(context, out FunctionParameter? parameter) is Result.Passable functionParameterResult)
			{
				if (functionParameterResult is Result.Failure)
					results.Add(functionParameterResult);
				parametersList.Add(parameter);
				if (!lexer.Eat<Comma>())
					break;
			}
			
			FunctionParameter? varArg = null;
			if (ParseTypeIdentifier(context, out TypedType? varArgType) is not Result.TokenRuleFailed &&
			    lexer.Eat<Ellipsis>() &&
			    lexer.Eat(out Word? varArgName))
				varArg = new FunctionParameter(varArgType, varArgName.TokenText);
			
			if (lexer.SkipTo<RightParenthesis>())
				results.Add(Result.ComplexTokenRuleFailed("Expected ')'", lexer.Line, lexer.Column));
			
			parameters = new FunctionParameters(parametersList, varArg);
			return Result.WrapPassable("Invalid function parameters", results.ToArray());
		}
		else
		{
			parameters = null;
			return new Result.TokenRuleFailed("Expected '('", lexer.Line, lexer.Column);
		}
	}
}