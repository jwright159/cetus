using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class DelegateDeclarationContext : IFunctionContext
{
	public string Name { get; set; }
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public IToken[]? Pattern { get; set; }
	public TypeIdentifierContext ReturnType { get; set; }
	public int LexerStartIndex { get; set; }
	public FunctionParametersContext ParameterContexts { get; set; }
	public float Priority { get; }
	public FunctionParameters Parameters { get; set; }
	
	public override string ToString() => $"{ReturnType} {Name}{ParameterContexts}";
}

public partial class Parser
{
	public Result ParseDelegateDeclarationFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Tokens.Delegate>() &&
		    ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
		    lexer.Eat(out Word? functionName) &&
		    ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult)
		{
			DelegateDeclarationContext function = new();
			function.Name = functionName.Value;
			function.ReturnType = returnType;
			function.ParameterContexts = parameters;
			function.LexerStartIndex = startIndex;
			program.Functions.Add(function);
			return Result.WrapPassable($"Invalid delegate declaration for '{function.Name}'", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected delegate declaration", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitDelegateDeclaration(IHasIdentifiers program, DelegateDeclarationContext function)
	{
		string name = function.Name;
		TypedType returnType = VisitTypeIdentifier(program, function.ReturnType);
		FunctionParameters parameters = function.Parameters = VisitFunctionParameters(program, function.ParameterContexts);
		function.Type = new FunctionCall(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type);
		function.Value = new TypedValueType(function.Type);
		program.Identifiers.Add(name, function.Value);
	}
}