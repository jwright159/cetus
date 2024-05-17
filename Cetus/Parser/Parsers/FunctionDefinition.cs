using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface IFunctionContext
{
	public IToken[]? Pattern { get; }
	public FunctionParametersContext ParameterContexts { get; }
}

public class FunctionDefinitionContext : IFunctionContext, IHasIdentifiers
{
	public string Name;
	public TypeIdentifierContext ReturnType;
	public FunctionParametersContext ParameterContexts { get; set; }
	public FunctionParameters Parameters { get; set; }
	public IToken[]? Pattern { get; set; }
	public int LexerStartIndex { get; set; }
	public List<IFunctionStatementContext> Statements;
	public Dictionary<string, TypedValue> Identifiers { get; set; }
	public ProgramContext Program { get; set; }
}

public partial class Parser
{
	public bool ParseFunctionDefinitionFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Word>() &&
		    lexer.Eat(out Word? functionName) &&
		    lexer.EatMatches<LeftParenthesis, RightParenthesis>() &&
		    lexer.EatMatches<LeftBrace, RightBrace>())
		{
			FunctionDefinitionContext functionDefinition = new();
			functionDefinition.Name = functionName.TokenText;
			functionDefinition.LexerStartIndex = startIndex;
			program.Functions.Add(functionDefinition, null);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseFunctionDeclaration(ProgramContext program, FunctionDefinitionContext functionDefinition)
	{
		lexer.Index = functionDefinition.LexerStartIndex;
		if (
			ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat<Word>() &&
			ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult)
		{
			functionDefinition.ReturnType = returnType;
			functionDefinition.ParameterContexts = parameters;
			functionDefinition.Program = program;
			return Result.WrapPassable($"Invalid function declaration for '{functionDefinition.Name}'", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected function declaration for '{functionDefinition.Name}'", lexer.Line, lexer.Column);
		}
	}
	
	public Result ParseFunctionDefinition(ProgramContext program, FunctionDefinitionContext functionDefinition)
	{
		lexer.Index = functionDefinition.LexerStartIndex;
		if (
			lexer.Eat<Word>() &&
			lexer.Eat<Word>() &&
			lexer.EatMatches<LeftParenthesis, RightParenthesis>() &&
			ParseFunctionBlock(program, out List<IFunctionStatementContext> statements) is Result.Passable functionBlockResult)
		{
			functionDefinition.Statements = statements;
			return Result.WrapPassable($"Invalid function definition for '{functionDefinition.Name}'", functionBlockResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected function definition for '{functionDefinition.Name}'", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitFunctionDefinition(ProgramContext program, FunctionDefinitionContext functionDefinition)
	{
		string name = functionDefinition.Name;
		TypedType returnType = VisitTypeIdentifier(program, functionDefinition.ReturnType);
		FunctionParameters parameters = functionDefinition.Parameters = VisitFunctionParameters(program, functionDefinition.ParameterContexts);
		TypedTypeFunctionCall functionType = new(functionDefinition.Name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg?.Type);
		TypedValue function = new TypedValueValue(functionType, module.AddFunction(name, functionType.LLVMType));
		LLVMValueRef functionValue = function.Value;
		program.Identifiers.Add(name, function);
		
		functionValue.Linkage = LLVMLinkage.LLVMExternalLinkage;
		
		functionDefinition.Identifiers = new Dictionary<string, TypedValue>(program.Identifiers);
		for (int i = 0; i < parameters.Parameters.Count; ++i)
		{
			string parameterName = parameters.Parameters[i].Name;
			TypedType parameterType = parameters.Parameters[i].Type;
			LLVMValueRef param = functionValue.GetParam((uint)i);
			param.Name = parameterName;
			functionDefinition.Identifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		builder.PositionAtEnd(functionValue.AppendBasicBlock("entry"));
		
		VisitFunctionBlock(functionDefinition, functionDefinition.Statements);
	}
}