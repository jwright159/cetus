using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	/// <summary>
	/// Collects all the type and function names and patterns, recursively
	/// </summary>
	public Result ParseProgramStatementFirstPass(ProgramContext program)
	{
		if (ParseIncludeLibrary(program) is Result.Passable includeLibraryResult)
		{
			lexer.Eat<Semicolon>();
			return includeLibraryResult;
		}
		if (ParseFunctionDefinitionFirstPass(program) is Result.Passable functionDefinitionResult)
		{
			lexer.Eat<Semicolon>();
			return functionDefinitionResult;
		}
		if (ParseExternFunctionDeclarationFirstPass(program) is Result.Passable externFunctionDeclarationResult)
		{
			lexer.Eat<Semicolon>();
			return externFunctionDeclarationResult;
		}
		if (ParseExternStructDeclarationFirstPass(program) is Result.Passable externStructDeclarationResult)
		{
			lexer.Eat<Semicolon>();
			return externStructDeclarationResult;
		}
		if (ParseDelegateDeclarationFirstPass(program) is Result.Passable delegateDeclarationResult)
		{
			lexer.Eat<Semicolon>();
			return delegateDeclarationResult;
		}
		if (ParseStructDefinitionFirstPass(program) is Result.Passable structDefinitionResult)
		{
			lexer.Eat<Semicolon>();
			return structDefinitionResult;
		}
		return new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column);
	}
	
	/// <summary>
	/// Sets up struct fields and registers their llvm types
	/// </summary>
	public Result ParseTypeStatementDefinition(ITypeContext type)
	{
		if (type is CompilerTypeContext)
			return new Result.Ok();
		if (type is ExternStructDeclarationContext externStructDeclaration)
			return ParseExternStructDefinition(externStructDeclaration);
		if (type is StructDefinitionContext structDefinition)
			return ParseStructDefinition(structDefinition);
		throw new Exception($"Unknown statement type {type.GetType()}");
	}
	
	/// <summary>
	/// Sets up function bodies
	/// </summary>
	public Result ParseFunctionStatementDefinition(IFunctionContext function)
	{
		if (function is CompilerFunctionContext)
			return new Result.Ok();
		if (function is LateCompilerFunctionContext)
			return new Result.Ok();
		if (function is ExternFunctionDeclarationContext)
			return new Result.Ok();
		if (function is DelegateDeclarationContext)
			return new Result.Ok();
		if (function is FunctionDefinitionContext functionDefinition)
			return ParseFunctionDefinition(functionDefinition);
		if (function is GetterContext)
			return new Result.Ok();
		throw new Exception($"Unknown statement type {function.GetType()}");
	}
}

public partial class Visitor
{
	public void VisitTypeStatement(IHasIdentifiers program, ITypeContext type)
	{
		switch (type)
		{
			case CompilerTypeContext:
				break;
			case ExternStructDeclarationContext externStructDeclaration:
				VisitExternStructDeclaration(program, externStructDeclaration);
				break;
			case StructDefinitionContext structDefinition:
				VisitStructDefinition(program, structDefinition);
				break;
			default:
				throw new Exception($"Unknown statement type {type.GetType()}");
		}
	}
	
	public void VisitFunctionStatement(IHasIdentifiers program, IFunctionContext function)
	{
		switch (function)
		{
			case CompilerFunctionContext:
				break;
			case LateCompilerFunctionContext:
				break;
			case FunctionDefinitionContext functionDefinition:
				VisitFunctionDefinition(program, functionDefinition);
				break;
			case ExternFunctionDeclarationContext externFunctionDeclaration:
				VisitExternFunctionDeclaration(program, externFunctionDeclaration);
				break;
			case DelegateDeclarationContext delegateDeclaration:
				VisitDelegateDeclaration(program, delegateDeclaration);
				break;
			case GetterContext getter:
				VisitGetter(getter);
				break;
			default:
				throw new Exception($"Unknown statement type {function.GetType()}");
		}
	}
}