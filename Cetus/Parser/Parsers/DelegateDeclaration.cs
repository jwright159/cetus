using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class DelegateDeclarationContext : IFunctionContext
{
	public string Name;
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
			lexer.Eat<Semicolon>();
			DelegateDeclarationContext delegateDeclaration = new();
			delegateDeclaration.Name = functionName.TokenText;
			delegateDeclaration.LexerStartIndex = startIndex;
			program.Functions.Add(delegateDeclaration, null);
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
			return Result.WrapPassable("Invalid delegate declaration", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed("Expected delegate declaration", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitDelegateDeclaration(ProgramContext program, DelegateDeclarationContext delegateDeclaration)
	{
		string name = delegateDeclaration.Name;
		TypedType returnType = VisitTypeIdentifier(program, delegateDeclaration.ReturnType);
		FunctionParameters parameters = delegateDeclaration.Parameters = VisitFunctionParameters(program, delegateDeclaration.ParameterContexts);
		TypedTypeFunction functionType = new(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type);
		TypedValue function = new TypedValueType(functionType);
		program.Identifiers.Add(name, function);
		program.Functions[delegateDeclaration] = function;
	}
}