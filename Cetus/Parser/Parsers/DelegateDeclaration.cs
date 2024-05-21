using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class DelegateDeclarationContext : IFunctionContext
{
	public string Name;
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public IToken[]? Pattern { get; set; }
	public int LexerStartIndex { get; set; }
	public TypeIdentifierContext ReturnType;
	public FunctionParametersContext ParameterContexts { get; set; }
	public FunctionParameters Parameters { get; set; }
}

public partial class Parser
{
	public bool ParseDelegateDeclarationFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Tokens.Delegate>() &&
			lexer.Eat<Word>() &&
		    lexer.Eat(out Word? functionName) &&
		    lexer.EatMatches<LeftParenthesis, RightParenthesis>())
		{
			DelegateDeclarationContext delegateDeclaration = new();
			delegateDeclaration.Name = functionName.TokenText;
			delegateDeclaration.LexerStartIndex = startIndex;
			program.Functions.Add(delegateDeclaration);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseDelegateDeclaration(DelegateDeclarationContext delegateDeclaration)
	{
		lexer.Index = delegateDeclaration.LexerStartIndex;
		if (
			lexer.Eat<Tokens.Delegate>() &&
			ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat<Word>() &&
			ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult)
		{
			delegateDeclaration.ReturnType = returnType;
			delegateDeclaration.ParameterContexts = parameters;
			return Result.WrapPassable($"Invalid delegate declaration for '{delegateDeclaration.Name}'", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected delegate declaration for '{delegateDeclaration.Name}'", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitDelegateDeclaration(ProgramContext program, DelegateDeclarationContext function)
	{
		string name = function.Name;
		TypedType returnType = VisitTypeIdentifier(program, function.ReturnType);
		FunctionParameters parameters = function.Parameters = VisitFunctionParameters(program, function.ParameterContexts);
		function.Type = new TypedTypeFunctionCall(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type);
		function.Value = new TypedValueType(function.Type);
		program.Identifiers.Add(name, function.Value);
	}
}