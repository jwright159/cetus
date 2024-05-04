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
		public List<IFunctionStatementContext> Statements = null!;
	}
	
	public Result ParseFunctionDefinition(out FunctionDefinitionContext? functionDefinition)
	{
		int startIndex = lexer.Index;
		if (
			ParseTypeIdentifier(out TypeIdentifierContext? returnType) is var returnTypeResult and not Result.TokenRuleFailed &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(out FunctionParametersContext? parameters) is var parametersResult and not Result.TokenRuleFailed &&
			ParseFunctionBlock(out FunctionBlockContext? functionBlock) is var functionBlockResult and not Result.TokenRuleFailed)
		{
			Result? result = null;
			functionDefinition = new FunctionDefinitionContext();
			functionDefinition.FunctionName = functionName.TokenText;
			functionDefinition.Parameters = parameters.Parameters;
			functionDefinition.IsVarArg = parameters.IsVarArg;
			functionDefinition.ReturnType = returnType;
			functionDefinition.Statements = functionBlock.Statements;
			return returnTypeResult as Result.ComplexRuleFailed as Result ??
			       parametersResult as Result.ComplexRuleFailed as Result ??
			       functionBlockResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
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
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ??
			       functionParametersResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
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
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ??
			       functionParametersResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
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
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ??
			       valueResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			constVariableDefinition = null;
			return new Result.TokenRuleFailed("Expected const variable definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public interface IValueContext : IExpressionContext;
	
	public Result? ParseValue(out IValueContext? value)
	{
		if (lexer.IsAtEnd)
		{
			value = null;
			return null;
		}
		else if (ParseHexInteger(out IntegerContext? hexInteger) is var hexIntegerResult and not Result.TokenRuleFailed)
		{
			value = hexInteger;
			return hexIntegerResult;
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
		else if (ParseDecimalInteger(out IntegerContext? decimalInteger) is var decimalIntegerResult and not Result.TokenRuleFailed)
		{
			value = decimalInteger;
			return decimalIntegerResult;
		}
		else if (ParseString(out StringContext? @string) is var stringResult and not Result.TokenRuleFailed)
		{
			value = @string;
			return stringResult;
		}
		else if (ParseNull(out NullContext? @null) is var nullResult and not Result.TokenRuleFailed)
		{
			value = @null;
			return nullResult;
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
	
	public Result ParseHexInteger(out IntegerContext? integer)
	{
		if (lexer.Eat(out HexInteger? hexIntegerToken))
		{
			integer = new IntegerContext();
			integer.Value = int.Parse(hexIntegerToken.TokenText[2..], NumberStyles.HexNumber);
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
	
	public Result ParseDecimalInteger(out IntegerContext? integer)
	{
		if (lexer.Eat(out DecimalInteger? decimalIntegerToken))
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
	
	
	public interface IFunctionStatementContext : IContext;
	
	public Result? ParseFunctionStatement(out IFunctionStatementContext? functionStatement)
	{
		if (lexer.IsAtEnd)
		{
			functionStatement = null;
			return null;
		}
		else if (ParseReturn(out ReturnContext? @return) is var returnResult and not Result.TokenRuleFailed)
		{
			functionStatement = @return;
			return returnResult;
		}
		else if (ParseFunctionCallStatement(out FunctionCallStatementContext? functionCall) is var functionCallResult and not Result.TokenRuleFailed)
		{
			functionStatement = functionCall.Call;
			return functionCallResult;
		}
		else if (ParseAssignment(out AssignmentContext? assignment) is var assignmentResult and not Result.TokenRuleFailed)
		{
			functionStatement = assignment;
			return assignmentResult;
		}
		else if (ParseIf(out IfContext? @if) is var ifResult and not Result.TokenRuleFailed)
		{
			functionStatement = @if;
			return ifResult;
		}
		else if (ParseWhile(out WhileContext? @while) is var whileResult and not Result.TokenRuleFailed)
		{
			functionStatement = @while;
			return whileResult;
		}
		else
		{
			functionStatement = null;
			return new Result.TokenRuleFailed("Expected function statement", lexer.Line, lexer.Column);
		}
	}
	
	
	public class ReturnContext : IFunctionStatementContext
	{
		public IExpressionContext? Value = null!;
	}
	
	public Result ParseReturn(out ReturnContext? @return)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Return>())
		{
			@return = new ReturnContext();
			
			Result? expressionResult = ParseExpression(out IExpressionContext? value);
			if (expressionResult is not Result.TokenRuleFailed)
				@return.Value = value;
			
			if (!lexer.Eat<Semicolon>())
				return new Result.ComplexRuleFailed("Expected ';'", new Result.TokenRuleFailed("Expected ';'", lexer.Line, lexer.Column));
			
			return expressionResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			@return = null;
			return new Result.TokenRuleFailed("Expected const variable definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public interface IExpressionContext : IContext;
	
	public Result? ParseExpression(out IExpressionContext? expression, int order = 0)
	{
		if (lexer.IsAtEnd)
		{
			expression = null;
			return null;
		}
		
		if (order <= 0)
		{
			if (ParseEquivalence(out EquivalenceContext? equivalence) is var equivalenceResult and not Result.TokenRuleFailed)
			{
				expression = equivalence;
				return equivalenceResult;
			}
			if (ParseInequivalence(out InequivalenceContext? inequivalence) is var inequivalenceResult and not Result.TokenRuleFailed)
			{
				expression = inequivalence;
				return inequivalenceResult;
			}
		}
		
		if (order <= 1)
		{
			if (ParseAddition(out AdditionContext? addition) is var additionResult and not Result.TokenRuleFailed)
			{
				expression = addition;
				return additionResult;
			}
		}
		
		if (order <= 2)
		{
			if (ParseNot(out NotContext? not) is var notResult and not Result.TokenRuleFailed)
			{
				expression = not;
				return notResult;
			}
			if (ParseDereference(out DereferenceContext? dereference) is var dereferenceResult and not Result.TokenRuleFailed)
			{
				expression = dereference;
				return dereferenceResult;
			}
		}
		
		if (order <= 3)
		{
			if (ParseFunctionCall(out FunctionCallContext? functionCall) is var functionCallResult and not Result.TokenRuleFailed)
			{
				expression = functionCall;
				return functionCallResult;
			}
		}
		
		if (order <= 4)
		{
			if (ParseValue(out IValueContext? value) is var valueResult and not Result.TokenRuleFailed)
			{
				expression = value;
				return valueResult;
			}
		}
		
		expression = null;
		return new Result.TokenRuleFailed("Expected function statement", lexer.Line, lexer.Column);
	}
	
	
	public class EquivalenceContext : IExpressionContext
	{
		public IExpressionContext Lhs = null!;
		public IExpressionContext Rhs = null!;
	}
	
	public Result ParseEquivalence(out EquivalenceContext? addition)
	{
		int startIndex = lexer.Index;
		if (
			ParseExpression(out IExpressionContext? lhs, 1) is var lhsResult and not Result.TokenRuleFailed &&
			lexer.Eat<Equivalence>() &&
			ParseExpression(out IExpressionContext? rhs, 1) is var rhsResult and not Result.TokenRuleFailed)
		{
			addition = new EquivalenceContext();
			addition.Lhs = lhs;
			addition.Rhs = rhs;
			return lhsResult as Result.ComplexRuleFailed as Result ??
			       rhsResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			addition = null;
			return new Result.TokenRuleFailed("Expected equivalence", lexer.Line, lexer.Column);
		}
	}
	
	
	public class InequivalenceContext : IExpressionContext
	{
		public IExpressionContext Lhs = null!;
		public IExpressionContext Rhs = null!;
	}
	
	public Result ParseInequivalence(out InequivalenceContext? addition)
	{
		int startIndex = lexer.Index;
		if (
			ParseExpression(out IExpressionContext? lhs, 1) is var lhsResult and not Result.TokenRuleFailed &&
			lexer.Eat<Inequivalence>() &&
			ParseExpression(out IExpressionContext? rhs, 1) is var rhsResult and not Result.TokenRuleFailed)
		{
			addition = new InequivalenceContext();
			addition.Lhs = lhs;
			addition.Rhs = rhs;
			return lhsResult as Result.ComplexRuleFailed as Result ??
			       rhsResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			addition = null;
			return new Result.TokenRuleFailed("Expected inequivalence", lexer.Line, lexer.Column);
		}
	}
	
	
	public class AdditionContext : IExpressionContext
	{
		public IExpressionContext Lhs = null!;
		public IExpressionContext Rhs = null!;
	}
	
	public Result ParseAddition(out AdditionContext? addition)
	{
		int startIndex = lexer.Index;
		if (
			ParseExpression(out IExpressionContext? lhs, 2) is var lhsResult and not Result.TokenRuleFailed &&
			lexer.Eat<Add>() &&
			ParseExpression(out IExpressionContext? rhs, 2) is var rhsResult and not Result.TokenRuleFailed)
		{
			addition = new AdditionContext();
			addition.Lhs = lhs;
			addition.Rhs = rhs;
			return lhsResult as Result.ComplexRuleFailed as Result ??
			       rhsResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			addition = null;
			return new Result.TokenRuleFailed("Expected addition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class NotContext : IExpressionContext
	{
		public IExpressionContext Value = null!;
	}
	
	public Result ParseNot(out NotContext? not)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Not>() &&
		    ParseExpression(out IExpressionContext? value, 3) is var valueResult and not Result.TokenRuleFailed)
		{
			not = new NotContext();
			not.Value = value;
			return valueResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			not = null;
			return new Result.TokenRuleFailed("Expected not", lexer.Line, lexer.Column);
		}
	}
	
	
	public class DereferenceContext : IExpressionContext
	{
		public IExpressionContext Value = null!;
	}
	
	public Result ParseDereference(out DereferenceContext? dereference)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Dereference>() &&
			ParseExpression(out IExpressionContext? value, 3) is var valueResult and not Result.TokenRuleFailed)
		{
			dereference = new DereferenceContext();
			dereference.Value = value;
			return valueResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			dereference = null;
			return new Result.TokenRuleFailed("Expected dereference", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionCallContext : IExpressionContext, IFunctionStatementContext
	{
		public IExpressionContext Function = null!;
		public List<IExpressionContext> Arguments = null!;
	}
	
	public Result ParseFunctionCall(out FunctionCallContext? functionCall)
	{
		int startIndex = lexer.Index;
		if (
			ParseExpression(out IExpressionContext? function, 4) is var functionResult and not Result.TokenRuleFailed &&
			ParseFunctionArguments(out FunctionArgumentsContext? arguments) is var functionArgumentsResult and not Result.TokenRuleFailed)
		{
			functionCall = new FunctionCallContext();
			functionCall.Function = function;
			functionCall.Arguments = arguments.Arguments;
			return functionResult as Result.ComplexRuleFailed as Result ??
			       functionArgumentsResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			functionCall = null;
			return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionArgumentsContext : IContext
	{
		public List<IExpressionContext> Arguments = [];
	}
	
	public Result ParseFunctionArguments(out FunctionArgumentsContext? arguments)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			Result? result = null;
			arguments = new FunctionArgumentsContext();
			while (ParseFunctionArgument(out FunctionArgumentContext? argument) is var functionParameterResult and not Result.TokenRuleFailed)
			{
				if (functionParameterResult is Result.ComplexRuleFailed)
					result ??= functionParameterResult;
				if (functionParameterResult is Result.Ok)
					arguments.Arguments.Add(argument.Value);
				if (!lexer.Eat<Comma>())
					break;
			}
			if (!lexer.Eat<RightParenthesis>())
			{
				result ??= new Result.ComplexRuleFailed("Expected ')'", new Result.TokenRuleFailed("Expected ')'", lexer.Line, lexer.Column));
				lexer.EatTo<RightParenthesis>();
			}
			return result ?? new Result.Ok();
		}
		else
		{
			arguments = null;
			return new Result.TokenRuleFailed("Expected '('", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionArgumentContext : IContext
	{
		public IExpressionContext Value = null!;
	}
	
	public Result ParseFunctionArgument(out FunctionArgumentContext? argument)
	{
		int startIndex = lexer.Index;
		if (ParseExpression(out IExpressionContext? value) is not Result.TokenRuleFailed)
		{
			argument = new FunctionArgumentContext();
			argument.Value = value;
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			argument = null;
			return new Result.TokenRuleFailed("Expected function argument", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionCallStatementContext : IFunctionStatementContext
	{
		public FunctionCallContext Call = null!;
	}
	
	public Result ParseFunctionCallStatement(out FunctionCallStatementContext? functionCallStatement)
	{
		int startIndex = lexer.Index;
		if (ParseFunctionCall(out FunctionCallContext? functionCall) is var functionCallResult and not Result.TokenRuleFailed &&
		    lexer.Eat<Semicolon>())
		{
			functionCallStatement = new FunctionCallStatementContext();
			functionCallStatement.Call = functionCall;
			return functionCallResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			functionCallStatement = null;
			return new Result.TokenRuleFailed("Expected const variable definition", lexer.Line, lexer.Column);
		}
	}
	
	
	public class AssignmentContext : IFunctionStatementContext
	{
		public string VariableName = null!;
		public TypeIdentifierContext Type = null!;
		public IExpressionContext Value = null!;
	}
	
	public Result ParseAssignment(out AssignmentContext? assignment)
	{
		int startIndex = lexer.Index;
		if (
			ParseTypeIdentifier(out TypeIdentifierContext? type) is var typeIdentifierResult and not Result.TokenRuleFailed &&
			lexer.Eat(out Word? variableName) &&
			lexer.Eat<Assign>() &&
			ParseExpression(out IExpressionContext? value) is var valueResult and not Result.TokenRuleFailed &&
			lexer.Eat<Semicolon>())
		{
			assignment = new AssignmentContext();
			assignment.VariableName = variableName.TokenText;
			assignment.Type = type;
			assignment.Value = value;
			return typeIdentifierResult as Result.ComplexRuleFailed as Result ??
			       valueResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			assignment = null;
			return new Result.TokenRuleFailed("Expected assignment", lexer.Line, lexer.Column);
		}
	}
	
	
	public class IfContext : IFunctionStatementContext
	{
		public IExpressionContext Condition = null!;
		public List<IFunctionStatementContext> ThenStatements = null!;
		public List<IFunctionStatementContext> ElseStatements = null!;
	}
	
	public Result ParseIf(out IfContext? @if)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<If>() &&
			lexer.Eat<LeftParenthesis>() &&
			ParseExpression(out IExpressionContext? expression) is var expressionResult and not Result.TokenRuleFailed &&
			lexer.Eat<RightParenthesis>() &&
			ParseFunctionBlock(out FunctionBlockContext? trueBlock) is var trueBlockResult and not Result.TokenRuleFailed &&
			lexer.Eat<Else>() &&
			ParseFunctionBlock(out FunctionBlockContext? falseBlock) is var falseBlockResult and not Result.TokenRuleFailed)
		{
			@if = new IfContext();
			@if.Condition = expression;
			@if.ThenStatements = trueBlock.Statements;
			@if.ElseStatements = falseBlock.Statements;
			return expressionResult as Result.ComplexRuleFailed as Result ??
			       trueBlockResult as Result.ComplexRuleFailed as Result ??
			       falseBlockResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		lexer.Index = startIndex;
		if (
			lexer.Eat<If>() &&
			lexer.Eat<LeftParenthesis>() &&
			ParseExpression(out IExpressionContext? expression2) is var expression2Result and not Result.TokenRuleFailed &&
			lexer.Eat<RightParenthesis>() &&
			ParseFunctionBlock(out FunctionBlockContext? block) is var blockResult and not Result.TokenRuleFailed)
		{
			@if = new IfContext();
			@if.Condition = expression2;
			@if.ThenStatements = block.Statements;
			@if.ElseStatements = [];
			return expression2Result as Result.ComplexRuleFailed as Result ??
			       blockResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		lexer.Index = startIndex;
		@if = null;
		return new Result.TokenRuleFailed("Expected assignment", lexer.Line, lexer.Column);
	}
	
	
	public class WhileContext : IFunctionStatementContext
	{
		public IExpressionContext Condition = null!;
		public List<IFunctionStatementContext> ThenStatements = null!;
	}
	
	public Result ParseWhile(out WhileContext? @while)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<While>() &&
			lexer.Eat<LeftParenthesis>() &&
			ParseExpression(out IExpressionContext? expression) is var expressionResult and not Result.TokenRuleFailed &&
			lexer.Eat<RightParenthesis>() &&
			ParseFunctionBlock(out FunctionBlockContext? block) is var blockResult and not Result.TokenRuleFailed)
		{
			@while = new WhileContext();
			@while.Condition = expression;
			@while.ThenStatements = block.Statements;
			return expressionResult as Result.ComplexRuleFailed as Result ??
			       blockResult as Result.ComplexRuleFailed as Result ??
			       new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			@while = null;
			return new Result.TokenRuleFailed("Expected while statement", lexer.Line, lexer.Column);
		}
	}
	
	
	public class FunctionBlockContext : IContext
	{
		public List<IFunctionStatementContext> Statements = null!;
	}
	
	public Result ParseFunctionBlock(out FunctionBlockContext? block)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			Result? result = null;
			block = new FunctionBlockContext();
			block.Statements = [];
			while (ParseFunctionStatement(out IFunctionStatementContext? statement) is var functionStatementResult and not Result.TokenRuleFailed)
			{
				if (functionStatementResult is Result.ComplexRuleFailed)
					result ??= new Result.ComplexRuleFailed("Invalid function statement", functionStatementResult);
				block.Statements.Add(statement);
			}
			
			if (!lexer.Eat<RightBrace>())
				result ??= new Result.ComplexRuleFailed("Expected '}'", new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column));
			
			return result ?? new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			block = null;
			return new Result.TokenRuleFailed("Expected if statement", lexer.Line, lexer.Column);
		}
	}
	
	
	public class NullContext : IValueContext;
	
	public Result ParseNull(out NullContext? @null)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Null>())
		{
			@null = new NullContext();
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			@null = null;
			return new Result.TokenRuleFailed("Expected null", lexer.Line, lexer.Column);
		}
	}
}