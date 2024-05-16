using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public class FunctionParametersContext
{
	public List<FunctionParameterContext> Parameters = [];
	public FunctionParameterContext? VarArg;
}

public class FunctionParameters
{
	public List<FunctionParameter> Parameters = [];
	public FunctionParameter? VarArg;
	
	public IEnumerable<TypedType> ParamTypes => Parameters.Select(p => p.Type);
}

public partial class Parser
{
	public Result ParseFunctionParameters(out FunctionParametersContext parameters)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			parameters = new FunctionParametersContext();
			List<Result> results = [];
			
			while (ParseFunctionParameter(out FunctionParameterContext? parameter) is Result.Passable functionParameterResult)
			{
				if (functionParameterResult is Result.Failure)
					results.Add(functionParameterResult);
				parameters.Parameters.Add(parameter);
				if (!lexer.Eat<Comma>())
					break;
			}
			
			if (ParseTypeIdentifier(out TypeIdentifierContext? varArgType) is not Result.TokenRuleFailed &&
			    lexer.Eat<Ellipsis>() &&
			    lexer.Eat(out Word? varArgName))
				parameters.VarArg = new FunctionParameterContext(varArgType, varArgName.TokenText);
			
			if (lexer.SkipTo<RightParenthesis>(out int line, out int column))
				results.Add(Result.ComplexTokenRuleFailed("Expected ')'", line, column));
			
			return Result.WrapPassable("Invalid function parameters", results.ToArray());
		}
		else
		{
			parameters = null;
			return new Result.TokenRuleFailed("Expected '('", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public FunctionParameters VisitFunctionParameters(ProgramContext program, FunctionParametersContext parameters)
	{
		FunctionParameters functionParameters = new();
		
		foreach (FunctionParameterContext parameter in parameters.Parameters)
			functionParameters.Parameters.Add(VisitFunctionParameter(program, parameter));
		
		if (parameters.VarArg is not null)
			functionParameters.VarArg = VisitFunctionParameter(program, parameters.VarArg);
		
		return functionParameters;
	}
}