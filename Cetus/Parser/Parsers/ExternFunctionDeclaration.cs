using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ExternFunctionDeclarationContext : IFunctionContext
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
	public bool ParseExternFunctionDeclarationFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Extern>() &&
			lexer.Eat<Word>() &&
		    lexer.Eat(out Word? functionName) &&
		    lexer.EatMatches<LeftParenthesis, RightParenthesis>())
		{
			ExternFunctionDeclarationContext externFunctionDeclaration = new();
			externFunctionDeclaration.Name = functionName.TokenText;
			externFunctionDeclaration.LexerStartIndex = startIndex;
			program.Functions.Add(externFunctionDeclaration);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseExternFunctionDeclaration(ExternFunctionDeclarationContext externFunctionDeclaration)
	{
		lexer.Index = externFunctionDeclaration.LexerStartIndex;
		if (
			lexer.Eat<Extern>() &&
			ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat<Word>() &&
			ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult)
		{
			externFunctionDeclaration.ReturnType = returnType;
			externFunctionDeclaration.ParameterContexts = parameters;
			return Result.WrapPassable($"Invalid extern function declaration for '{externFunctionDeclaration.Name}'", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected extern function declaration for '{externFunctionDeclaration.Name}'", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitExternFunctionDeclaration(ProgramContext program, ExternFunctionDeclarationContext function)
	{
		string name = function.Name;
		TypedType returnType = VisitTypeIdentifier(program, function.ReturnType);
		FunctionParameters parameters = function.Parameters = VisitFunctionParameters(program, function.ParameterContexts);
		function.Type = new TypedTypeFunctionCall(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type);
		LLVMValueRef functionValue = module.AddFunction(name, function.Type.LLVMType);
		function.Value = new TypedValueValue(function.Type, functionValue);
		program.Identifiers.Add(name, function.Value);
	}
}