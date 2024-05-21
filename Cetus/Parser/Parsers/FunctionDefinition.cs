using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface IFunctionContext
{
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken[]? Pattern { get; }
	public FunctionParametersContext ParameterContexts { get; }
}

public class FunctionDefinitionContext : IFunctionContext, IHasIdentifiers
{
	public string Name;
	public TypeIdentifierContext ReturnType;
	public FunctionParametersContext ParameterContexts { get; set; }
	public FunctionParameters Parameters { get; set; }
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public IToken[]? Pattern { get; set; }
	public int LexerStartIndex { get; set; }
	public List<IFunctionStatementContext> Statements;
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
}

public partial class Parser
{
	public bool ParseFunctionDefinitionFirstPass(IHasIdentifiers program)
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
			functionDefinition.NestFrom(program);
			program.Functions.Add(functionDefinition);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseFunctionDeclaration(FunctionDefinitionContext functionDefinition)
	{
		lexer.Index = functionDefinition.LexerStartIndex;
		if (
			ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat<Word>() &&
			ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult)
		{
			functionDefinition.ReturnType = returnType;
			functionDefinition.ParameterContexts = parameters;
			return Result.WrapPassable($"Invalid function declaration for '{functionDefinition.Name}'", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected function declaration for '{functionDefinition.Name}'", lexer.Line, lexer.Column);
		}
	}
	
	public Result ParseFunctionDefinition(FunctionDefinitionContext functionDefinition)
	{
		lexer.Index = functionDefinition.LexerStartIndex;
		if (
			lexer.Eat<Word>() &&
			lexer.Eat<Word>() &&
			lexer.EatMatches<LeftParenthesis, RightParenthesis>() &&
			ParseFunctionBlock(functionDefinition, out List<IFunctionStatementContext> statements) is Result.Passable functionBlockResult)
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
	public void VisitFunctionDefinition(ProgramContext program, FunctionDefinitionContext function)
	{
		string name = function.Name;
		TypedType returnType = VisitTypeIdentifier(program, function.ReturnType);
		FunctionParameters parameters = function.Parameters = VisitFunctionParameters(program, function.ParameterContexts);
		function.Type = new TypedTypeFunctionCall(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg?.Type);
		LLVMValueRef functionValue = module.AddFunction(name, function.Type.LLVMType);
		function.Value = new TypedValueValue(function.Type, functionValue);
		program.Identifiers.Add(name, function.Value);
		
		functionValue.Linkage = LLVMLinkage.LLVMExternalLinkage;
		
		for (int i = 0; i < parameters.Parameters.Count; ++i)
		{
			string parameterName = parameters.Parameters[i].Name;
			TypedType parameterType = parameters.Parameters[i].Type;
			LLVMValueRef param = functionValue.GetParam((uint)i);
			param.Name = parameterName;
			function.Identifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		builder.PositionAtEnd(functionValue.AppendBasicBlock("entry"));
		
		VisitFunctionBlock(function, function.Statements);
	}
}