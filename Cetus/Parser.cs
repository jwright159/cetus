using System.Globalization;
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
	
	
	public interface IProgramStatementContext : IContext;
	
	public Result? ParseProgramStatement(out IProgramStatementContext? programStatement)
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
		else if (ParseConstVariableDefinition(out ConstVariableDefinitionContext? constVariableDefinition) is var constVariableDefinitionResult and not Result.TokenRuleFailed)
		{
			programStatement = constVariableDefinition;
			return constVariableDefinitionResult;
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
			return new Result.TokenRuleFailed("Expected extern function declaration", lexer.Line, lexer.Column);
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
			return new Result.TokenRuleFailed("Expected extern struct declaration", lexer.Line, lexer.Column);
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
			return new Result.TokenRuleFailed("Expected delegate declaration", lexer.Line, lexer.Column);
		}
	}
	
	
	public class ConstVariableDefinitionContext : IProgramStatementContext
	{
		public string VariableName = null!;
		public TypeIdentifierContext Type = null!;
		public IValueContext Value = null!;
	}
	
	public Result ParseConstVariableDefinition(out ConstVariableDefinitionContext? constVariableDefinition)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Constant>() &&
			ParseTypeIdentifier(out TypeIdentifierContext? type) is var typeIdentifierResult and not Result.TokenRuleFailed &&
			lexer.Eat(out Word? variableName) &&
			lexer.Eat<Assign>() &&
			ParseValue(out IValueContext? value) is var valueResult and not Result.TokenRuleFailed &&
			lexer.Eat<Semicolon>())
		{
			constVariableDefinition = new ConstVariableDefinitionContext();
			constVariableDefinition.VariableName = variableName.TokenText;
			constVariableDefinition.Type = type;
			constVariableDefinition.Value = value;
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ?? valueResult as Result.ComplexRuleFailed as Result ?? new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			constVariableDefinition = null;
			return new Result.TokenRuleFailed("Expected const variable definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public interface IValueContext : IContext;
	
	public Result? ParseValue(out IValueContext? value)
	{
		if (lexer.IsAtEnd)
		{
			value = null;
			return null;
		}
		else if (ParseInteger(out IntegerContext? integer) is var integerResult and not Result.TokenRuleFailed)
		{
			value = integer;
			return integerResult;
		}
		else if (ParseFloat(out FloatContext? @float) is var floatResult and not Result.TokenRuleFailed)
		{
			value = @float;
			return floatResult;
		}
		else if (ParseDouble(out DoubleContext? @double) is var doubleResult and not Result.TokenRuleFailed)
		{
			value = @double;
			return doubleResult;
		}
		else if (ParseString(out StringContext? @string) is var stringResult and not Result.TokenRuleFailed)
		{
			value = @string;
			return stringResult;
		}
		else if (ParseValueIdentifier(out ValueIdentifierContext? valueIdentifier) is var valueIdentifierResult and not Result.TokenRuleFailed)
		{
			value = valueIdentifier;
			return valueIdentifierResult;
		}
		else
		{
			value = null;
			return new Result.TokenRuleFailed("Expected value", lexer.Line, lexer.Column);
		}
	}
	
	
	public class IntegerContext : IValueContext
	{
		public int Value;
	}
	
	public Result ParseInteger(out IntegerContext? integer)
	{
		if (lexer.Eat(out HexInteger? hexIntegerToken))
		{
			integer = new IntegerContext();
			integer.Value = int.Parse(hexIntegerToken.TokenText[2..], NumberStyles.HexNumber);
			return new Result.Ok();
		}
		else if (lexer.Eat(out DecimalInteger? decimalIntegerToken))
		{
			integer = new IntegerContext();
			integer.Value = int.Parse(decimalIntegerToken.TokenText);
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FloatContext : IValueContext
	{
		public float Value;
	}
	
	public Result ParseFloat(out FloatContext? @float)
	{
		if (lexer.Eat(out Float? floatToken))
		{
			@float = new FloatContext();
			@float.Value = float.Parse(floatToken.TokenText[..^1]);
			return new Result.Ok();
		}
		else
		{
			@float = null;
			return new Result.TokenRuleFailed("Expected float", lexer.Line, lexer.Column);
		}
	}
	
	
	public class DoubleContext : IValueContext
	{
		public float Value;
	}
	
	public Result ParseDouble(out DoubleContext? @double)
	{
		if (lexer.Eat(out Tokens.Double? doubleToken))
		{
			@double = new DoubleContext();
			@double.Value = float.Parse(doubleToken.TokenText);
			return new Result.Ok();
		}
		else
		{
			@double = null;
			return new Result.TokenRuleFailed("Expected double", lexer.Line, lexer.Column);
		}
	}
	
	
	public class StringContext : IValueContext
	{
		public string Value = null!;
	}
	
	public Result ParseString(out StringContext? @string)
	{
		if (lexer.Eat(out Tokens.String? stringToken))
		{
			@string = new StringContext();
			@string.Value = stringToken.TokenText[1..^1];
			@string.Value = System.Text.RegularExpressions.Regex.Unescape(@string.Value);
			return new Result.Ok();
		}
		else
		{
			@string = null;
			return new Result.TokenRuleFailed("Expected string", lexer.Line, lexer.Column);
		}
	}
	
	
	public class TypeIdentifierContext : IContext
	{
		public string TypeName = null!;
		public int PointerCount;
	}
	
	public Result ParseTypeIdentifier(out TypeIdentifierContext? typeIdentifier)
	{
		if (lexer.Eat(out Word? typeName))
		{
			typeIdentifier = new TypeIdentifierContext();
			typeIdentifier.TypeName = typeName.TokenText;
			while (lexer.Eat<Dereference>())
				typeIdentifier.PointerCount++;
			return new Result.Ok();
		}
		else
		{
			typeIdentifier = null;
			return new Result.TokenRuleFailed("Expected type identifier", lexer.Line, lexer.Column);
		}
	}
	
	
	public class ValueIdentifierContext : IValueContext
	{
		public string ValueName = null!;
	}
	
	public Result ParseValueIdentifier(out ValueIdentifierContext? valueIdentifier)
	{
		if (lexer.Eat(out Word? valueName))
		{
			valueIdentifier = new ValueIdentifierContext();
			valueIdentifier.ValueName = valueName.TokenText;
			return new Result.Ok();
		}
		else
		{
			valueIdentifier = null;
			return new Result.TokenRuleFailed("Expected value identifier", lexer.Line, lexer.Column);
		}
	}
}