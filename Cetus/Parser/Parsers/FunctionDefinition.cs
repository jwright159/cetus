using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface IFunctionContext
{
	public string Name { get; }
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken? Pattern { get; }
	public TypeIdentifierContext ReturnType { get; }
	public FunctionParametersContext ParameterContexts { get; }
	public float Priority { get; }
}

public static class IFunctionContextExtensions
{
	public static int IndexOf(this IFunctionContext function, ParameterToken token) => function.ParameterContexts.Parameters.FindIndex(parameter => parameter.Name == token.ParameterName);
}

public class FunctionDefinitionContext : IFunctionContext, IHasIdentifiers
{
	public string Name { get; set; }
	public TypeIdentifierContext ReturnType { get; set; }
	public FunctionParametersContext ParameterContexts { get; set; }
	public float Priority { get; }
	public FunctionParameters Parameters { get; set; }
	public TypedType? Type { get; set; }
	public TypedValue? Value { get; set; }
	public IToken? Pattern { get; set; }
	public int LexerBlockStartIndex { get; set; }
	public List<IFunctionStatementContext> Statements;
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
	
	public override string ToString() => $"{ReturnType} {Name}{ParameterContexts}";
}

public partial class Parser
{
	public Result ParseFunctionDefinitionFirstPass(IHasIdentifiers program)
	{
		int startIndex = lexer.Index;
		int blockStartIndex = -1;
		if (ParseTypeIdentifier(out TypeIdentifierContext returnType) is Result.Passable typeIdentifierResult &&
		    lexer.Eat(out Word? functionName) &&
		    ParseFunctionParameters(out FunctionParametersContext parameters) is Result.Passable functionParametersResult &&
		    Return.True(() => blockStartIndex = lexer.Index) &&
		    lexer.EatMatches<LeftBrace, RightBrace>())
		{
			FunctionDefinitionContext function = new();
			function.Name = functionName.Value;
			function.ReturnType = returnType;
			function.ParameterContexts = parameters;
			function.LexerBlockStartIndex = blockStartIndex;
			function.NestFrom(program);
			program.Functions.Add(function);
			return Result.WrapPassable($"Invalid function declaration for '{function.Name}'", typeIdentifierResult, functionParametersResult);
		}
		lexer.Index = startIndex;
		return new Result.TokenRuleFailed("Expected function definition", lexer.Line, lexer.Column);
	}
	
	public Result ParseFunctionDefinition(FunctionDefinitionContext function)
	{
		lexer.Index = function.LexerBlockStartIndex;
		if (ParseFunctionBlock(function, out List<IFunctionStatementContext> statements) is Result.Passable functionBlockResult)
		{
			function.Statements = statements;
			return Result.WrapPassable($"Invalid function definition for '{function.Name}'", functionBlockResult);
		}
		
		return new Result.TokenRuleFailed($"Expected function definition for '{function.Name}'", lexer.Line, lexer.Column);
	}
}

public partial class Visitor
{
	public void VisitFunctionDefinition(IHasIdentifiers program, FunctionDefinitionContext function)
	{
		string name = function.Name;
		TypedType returnType = VisitTypeIdentifier(program, function.ReturnType);
		FunctionParameters parameters = function.Parameters = VisitFunctionParameters(program, function.ParameterContexts);
		function.Type = new FunctionCall(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg?.Type);
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