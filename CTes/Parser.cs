using CTes.Tokens;

namespace CTes;

public class Parser(Lexer lexer)
{
	public enum ParseResult
	{
		Ok, TokenRuleFailed, ComplexRuleFailed
	}
	
	public interface IContext;
	
	public class ProgramContext : IContext
	{
		public List<IProgramStatementContext> ProgramStatements = [];
	}
	
	public ProgramContext Parse()
	{
		ProgramContext program = new();
		while (ParseProgramStatement(out IProgramStatementContext? programStatement) is not ParseResult.TokenRuleFailed)
			program.ProgramStatements.Add(programStatement);
		return program;
	}
	
	
	// includeLibrary
	// externFunctionDeclaration
	// externStructDeclaration
	// externVariableDeclaration
	// constVariableDeclaration
	// delegateDeclaration
	// functionDefinition
	public interface IProgramStatementContext : IContext;
	
	private ParseResult ParseProgramStatement(out IProgramStatementContext? programStatement)
	{
		if (ParseIncludeLibrary(out IncludeLibraryContext? includeLibrary) is var includeLibraryResult and not ParseResult.TokenRuleFailed)
		{
			programStatement = includeLibrary;
			return includeLibraryResult;
		}
		else if (ParseFunctionDefinition(out FunctionDefinitionContext? functionDefinition) is var functionDefinitionResult and not ParseResult.TokenRuleFailed)
		{
			programStatement = functionDefinition;
			return functionDefinitionResult;
		}
		else
		{
			programStatement = null;
			return ParseResult.TokenRuleFailed;
		}
	}
	
	
	public class IncludeLibraryContext : IProgramStatementContext
	{
		public string LibraryName = null!;
	}
	
	public ParseResult ParseIncludeLibrary(out IncludeLibraryContext? includeLibrary)
	{
		if (lexer.Eat<Include>())
		{
			ParseResult result = ParseResult.Ok;
			includeLibrary = new IncludeLibraryContext();
			includeLibrary.LibraryName = lexer.Eat(out Word? libraryName) ? libraryName.TokenText : "";
			if (!lexer.Eat<Semicolon>())
				result = ParseResult.ComplexRuleFailed;
			return result;
		}
		else
		{
			includeLibrary = null;
			return ParseResult.TokenRuleFailed;
		}
	}
	
	
	public class FunctionDefinitionContext : IProgramStatementContext
	{
		public string FunctionName = null!;
		public List<FunctionParameterContext> Parameters = null!;
		public TypeIdentifierContext ReturnType = null!;
		// public List<FunctionStatementContext> Statements = null!;
	}
	
	public ParseResult ParseFunctionDefinition(out FunctionDefinitionContext? includeLibrary)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext? returnType) is not ParseResult.TokenRuleFailed && lexer.Eat(out Word? functionName) && ParseFunctionParameters(out FunctionParametersContext? parameters) is not ParseResult.TokenRuleFailed && lexer.Eat<LeftBrace>())
		{
			ParseResult result = ParseResult.Ok;
			FunctionDefinitionContext functionDefinition = new();
			functionDefinition.FunctionName = functionName.TokenText;
			functionDefinition.Parameters = parameters.Parameters;
			functionDefinition.ReturnType = returnType;
			// functionDefinition.Statements = [];
			// while (ParseFunctionStatement(out FunctionStatementContext? statement) is var functionStatementResult and not ParseResult.TokenRuleFailed)
			// {
			// 	if (functionStatementResult is ParseResult.ComplexRuleFailed)
			// 		result = ParseResult.ComplexRuleFailed;
			// 	
			// 	functionDefinition.Statements.Add(statement);
			// }
			if (!lexer.Eat<RightBrace>())
				result = ParseResult.ComplexRuleFailed;
			includeLibrary = functionDefinition;
			return result;
		}
		else
		{
			lexer.Index = startIndex;
			includeLibrary = null;
			return ParseResult.TokenRuleFailed;
		}
	}
	
	
	public class FunctionParametersContext : IContext
	{
		public List<FunctionParameterContext> Parameters = [];
	}
	
	public ParseResult ParseFunctionParameters(out FunctionParametersContext? parameters)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			ParseResult result = ParseResult.Ok;
			parameters = new FunctionParametersContext();
			while (ParseFunctionParameter(out FunctionParameterContext? parameter) is var functionParameterResult and not ParseResult.TokenRuleFailed)
			{
				if (functionParameterResult is ParseResult.ComplexRuleFailed)
					result = ParseResult.ComplexRuleFailed;
				
				parameters.Parameters.Add(parameter);
				if (!lexer.Eat<Comma>())
					break;
			}
			if (!lexer.Eat<RightParenthesis>())
				result = ParseResult.ComplexRuleFailed;
			return result;
		}
		else
		{
			parameters = null;
			return ParseResult.TokenRuleFailed;
		}
	}
	
	
	public class FunctionParameterContext : IContext
	{
		public TypeIdentifierContext ParameterType = null!;
		public string ParameterName = null!;
	}
	
	public ParseResult ParseFunctionParameter(out FunctionParameterContext? parameter)
	{
		if (ParseTypeIdentifier(out TypeIdentifierContext? parameterType) is not ParseResult.TokenRuleFailed && lexer.Eat(out Word? parameterName))
		{
			parameter = new FunctionParameterContext();
			parameter.ParameterType = parameterType;
			parameter.ParameterName = parameterName.TokenText;
			return ParseResult.Ok;
		}
		else
		{
			parameter = null;
			return ParseResult.TokenRuleFailed;
		}
	}
	
	
	public class TypeIdentifierContext : IContext
	{
		public string TypeName = null!;
	}
	
	public ParseResult ParseTypeIdentifier(out TypeIdentifierContext? typeIdentifier)
	{
		if (lexer.Eat(out Word? typeName))
		{
			typeIdentifier = new TypeIdentifierContext();
			typeIdentifier.TypeName = typeName.TokenText;
			return ParseResult.Ok;
		}
		else
		{
			typeIdentifier = null;
			return ParseResult.TokenRuleFailed;
		}
	}
}