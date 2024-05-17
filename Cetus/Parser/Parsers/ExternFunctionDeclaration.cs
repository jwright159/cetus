using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ExternFunctionDeclarationContext : IFunctionContext
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
			program.Functions.Add(externFunctionDeclaration, null);
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
			return Result.WrapPassable("Invalid extern function declaration", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed("Expected extern function declaration", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitExternFunctionDeclaration(ProgramContext program, ExternFunctionDeclarationContext externFunctionDeclaration)
	{
		string name = externFunctionDeclaration.Name;
		TypedType returnType = VisitTypeIdentifier(program, externFunctionDeclaration.ReturnType);
		FunctionParameters parameters = externFunctionDeclaration.Parameters = VisitFunctionParameters(program, externFunctionDeclaration.ParameterContexts);
		TypedTypeFunction functionType = new(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type);
		LLVMValueRef functionValue = module.AddFunction(name, functionType.LLVMType);
		TypedValue function = new TypedValueValue(functionType, functionValue);
		program.Identifiers.Add(name, function);
		program.Functions[externFunctionDeclaration] = function;
	}
}