using Cetus.Tokens;

namespace Cetus;

public class Parser(Lexer lexer)
{
	public interface Result
	{
		public class Ok : Result
		{
			public override string ToString() => "Ok";
		}
		
		public class TokenRuleFailed(string message, int line, int column) : Result
		{
			public override string ToString() => $"TokenRule failed at ({line}, {column}): {message}";
		}
		
		public class ComplexRuleFailed(string message, Result result) : Result
		{
			public override string ToString() => $"ComplexRule failed: {message}\n\tAt {result}";
		}
	}
	
	
	public interface IContext;
	
	public class ProgramContext : IContext
	{
		public List<IProgramStatementContext> ProgramStatements = [];
	}
	
	public ProgramContext Parse()
	{
		ProgramContext program = new();
		while (ParseProgramStatement(out IProgramStatementContext? programStatement) is { } statementResult)
		{
			if (statementResult is Result.ComplexRuleFailed or Result.TokenRuleFailed)
				Console.WriteLine(statementResult);
			if (statementResult is Result.Ok or Result.ComplexRuleFailed)
				program.ProgramStatements.Add(programStatement);
			else
				break;
		}
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
	
	private Result? ParseProgramStatement(out IProgramStatementContext? programStatement)
	{
		if (lexer.IsAtEnd)
		{
			programStatement = null;
			return null;
		}
		else if (ParseIncludeLibrary(out IncludeLibraryContext? includeLibrary) is var includeLibraryResult and not Result.TokenRuleFailed)
		{
			programStatement = includeLibrary;
			return includeLibraryResult;
		}
		else if (ParseFunctionDefinition(out FunctionDefinitionContext? functionDefinition) is var functionDefinitionResult and not Result.TokenRuleFailed)
		{
			programStatement = functionDefinition;
			return functionDefinitionResult;
		}
		else if (ParseExternFunctionDeclaration(out ExternFunctionDeclarationContext? externFunctionDeclaration) is var externFunctionDeclarationResult and not Result.TokenRuleFailed)
		{
			programStatement = externFunctionDeclaration;
			return externFunctionDeclarationResult;
		}
		else if (ParseExternStructDeclaration(out ExternStructDeclarationContext? externStructDeclaration) is var externStructDeclarationResult and not Result.TokenRuleFailed)
		{
			programStatement = externStructDeclaration;
			return externStructDeclarationResult;
		}
		else if (ParseDelegateDeclaration(out DelegateDeclarationContext? delegateDeclaration) is var delegateDeclarationResult and not Result.TokenRuleFailed)
		{
			programStatement = delegateDeclaration;
			return delegateDeclarationResult;
		}
		else
		{
			programStatement = null;
			return new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column);
		}
	}
	
	
	public class IncludeLibraryContext : IProgramStatementContext
	{
		public string LibraryName = null!;
	}
	
	public Result ParseIncludeLibrary(out IncludeLibraryContext? includeLibrary)
	{
		if (lexer.Eat<Include>())
		{
			Result? result = null;
			includeLibrary = new IncludeLibraryContext();
			includeLibrary.LibraryName = lexer.Eat(out Word? libraryName) ? libraryName.TokenText : "";
			if (!lexer.Eat<Semicolon>())
				result ??= new Result.ComplexRuleFailed("Expected ';'", new Result.TokenRuleFailed("Expected ';'", lexer.Line, lexer.Column));
			return result ?? new Result.Ok();
		}
		else
		{
			includeLibrary = null;
			return new Result.TokenRuleFailed("Expected 'include'", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionDefinitionContext : IProgramStatementContext
	{
		public string FunctionName = null!;
		public List<FunctionParameterContext> Parameters = null!;
		public bool IsVarArg;
		public TypeIdentifierContext ReturnType = null!;
		// public List<FunctionStatementContext> Statements = null!;
	}
	
	public Result ParseFunctionDefinition(out FunctionDefinitionContext? functionDefinition)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext? returnType) is not Result.TokenRuleFailed && lexer.Eat(out Word? functionName) && ParseFunctionParameters(out FunctionParametersContext? parameters) is not Result.TokenRuleFailed && lexer.Eat<LeftBrace>())
		{
			Result? result = null;
			functionDefinition = new FunctionDefinitionContext();
			functionDefinition.FunctionName = functionName.TokenText;
			functionDefinition.Parameters = parameters.Parameters;
			functionDefinition.IsVarArg = parameters.IsVarArg;
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
				result ??= new Result.ComplexRuleFailed("Expected '}'", new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column));
			return result ?? new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			functionDefinition = null;
			return new Result.TokenRuleFailed("Expected function definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class ExternFunctionDeclarationContext : IProgramStatementContext
	{
		public string FunctionName = null!;
		public List<FunctionParameterContext> Parameters = null!;
		public bool IsVarArg;
		public TypeIdentifierContext ReturnType = null!;
	}
	
	public Result ParseExternFunctionDeclaration(out ExternFunctionDeclarationContext? externFunctionDeclaration)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			ParseTypeIdentifier(out TypeIdentifierContext? returnType) is var typeIdentifierResult and not Result.TokenRuleFailed &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(out FunctionParametersContext? parameters) is var functionParametersResult and not Result.TokenRuleFailed &&
			lexer.Eat<Semicolon>())
		{
			externFunctionDeclaration = new ExternFunctionDeclarationContext();
			externFunctionDeclaration.FunctionName = functionName.TokenText;
			externFunctionDeclaration.Parameters = parameters.Parameters;
			externFunctionDeclaration.IsVarArg = parameters.IsVarArg;
			externFunctionDeclaration.ReturnType = returnType;
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ?? functionParametersResult as Result.ComplexRuleFailed as Result ?? new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			externFunctionDeclaration = null;
			return new Result.TokenRuleFailed("Expected extern function definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class ExternStructDeclarationContext : IProgramStatementContext
	{
		public string StructName = null!;
	}
	
	public Result ParseExternStructDeclaration(out ExternStructDeclarationContext? externFunctionDeclaration)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			lexer.Eat<Struct>() &&
			lexer.Eat(out Word? structName) &&
			lexer.Eat<Semicolon>())
		{
			externFunctionDeclaration = new ExternStructDeclarationContext();
			externFunctionDeclaration.StructName = structName.TokenText;
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			externFunctionDeclaration = null;
			return new Result.TokenRuleFailed("Expected extern function definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionParametersContext : IContext
	{
		public List<FunctionParameterContext> Parameters = [];
		public bool IsVarArg;
	}
	
	public Result ParseFunctionParameters(out FunctionParametersContext? parameters)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			Result? result = null;
			parameters = new FunctionParametersContext();
			while (ParseFunctionParameter(out FunctionParameterContext? parameter) is var functionParameterResult and not Result.TokenRuleFailed)
			{
				if (functionParameterResult is Result.ComplexRuleFailed)
					result ??= functionParameterResult;
				if (functionParameterResult is Result.Ok)
					parameters.Parameters.Add(parameter);
				if (!lexer.Eat<Comma>())
					break;
			}
			if (lexer.Eat<Ellipsis>())
				parameters.IsVarArg = true;
			if (!lexer.Eat<RightParenthesis>())
			{
				result ??= new Result.ComplexRuleFailed("Expected ')'", new Result.TokenRuleFailed("Expected ')'", lexer.Line, lexer.Column));
				lexer.EatTo<RightParenthesis>();
			}
			return result ?? new Result.Ok();
		}
		else
		{
			parameters = null;
			return new Result.TokenRuleFailed("Expected '('", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionParameterContext : IContext
	{
		public TypeIdentifierContext ParameterType = null!;
		public string ParameterName = null!;
	}
	
	public Result ParseFunctionParameter(out FunctionParameterContext? parameter)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext? parameterType) is not Result.TokenRuleFailed && lexer.Eat(out Word? parameterName))
		{
			parameter = new FunctionParameterContext();
			parameter.ParameterType = parameterType;
			parameter.ParameterName = parameterName.TokenText;
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			parameter = null;
			return new Result.TokenRuleFailed("Expected function parameter", lexer.Line, lexer.Column);
		}
	}
	
	
	public class DelegateDeclarationContext : IProgramStatementContext
	{
		public string FunctionName = null!;
		public List<FunctionParameterContext> Parameters = null!;
		public bool IsVarArg;
		public TypeIdentifierContext ReturnType = null!;
	}
	
	public Result ParseDelegateDeclaration(out DelegateDeclarationContext? delegateDeclaration)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Tokens.Delegate>() &&
			ParseTypeIdentifier(out TypeIdentifierContext? returnType) is var typeIdentifierResult and not Result.TokenRuleFailed &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(out FunctionParametersContext? parameters) is var functionParametersResult and not Result.TokenRuleFailed &&
			lexer.Eat<Semicolon>())
		{
			delegateDeclaration = new DelegateDeclarationContext();
			delegateDeclaration.FunctionName = functionName.TokenText;
			delegateDeclaration.Parameters = parameters.Parameters;
			delegateDeclaration.IsVarArg = parameters.IsVarArg;
			delegateDeclaration.ReturnType = returnType;
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ?? functionParametersResult as Result.ComplexRuleFailed as Result ?? new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			delegateDeclaration = null;
			return new Result.TokenRuleFailed("Expected extern function definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class TypeIdentifierContext : IContext
	{
		public string TypeName = null!;
	}
	
	public Result ParseTypeIdentifier(out TypeIdentifierContext? typeIdentifier)
	{
		if (lexer.Eat(out Word? typeName))
		{
			typeIdentifier = new TypeIdentifierContext();
			typeIdentifier.TypeName = typeName.TokenText;
			return new Result.Ok();
		}
		else
		{
			typeIdentifier = null;
			return new Result.TokenRuleFailed("Expected type identifier", lexer.Line, lexer.Column);
		}
	}
}