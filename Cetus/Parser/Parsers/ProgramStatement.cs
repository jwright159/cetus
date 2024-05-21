using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	/// <summary>
	/// Collects all the type and function names and patterns, recursively
	/// </summary>
	public Result ParseProgramStatementFirstPass(ProgramContext program)
	{
		if (ParseIncludeLibrary(program) ||
		    ParseFunctionDefinitionFirstPass(program) ||
		    ParseExternFunctionDeclarationFirstPass(program) ||
		    ParseExternStructDeclarationFirstPass(program) ||
		    ParseDelegateDeclarationFirstPass(program) ||
		    ParseStructDefinitionFirstPass(program))
		{
			lexer.Eat<Semicolon>();
			return new Result.Ok();
		}
		else
			return new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column);
	}
	
	/// <summary>
	/// Unsure!
	/// </summary>
	public Result ParseTypeStatementDeclaration(ITypeContext type)
	{
		if (type is CompilerTypeContext)
			return new Result.Ok();
		if (type is ExternStructDeclarationContext)
			return new Result.Ok();
		if (type is StructDefinitionContext)
			return new Result.Ok();
		throw new Exception($"Unknown statement type {type}");
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
		throw new Exception($"Unknown statement type {type}");
	}
	
	/// <summary>
	/// Sets up function parameters and return types
	/// </summary>
	public Result ParseFunctionStatementDeclaration(IFunctionContext function)
	{
		if (function is CompilerFunctionContext)
			return new Result.Ok();
		if (function is ExternFunctionDeclarationContext externFunctionDeclaration)
			return ParseExternFunctionDeclaration(externFunctionDeclaration);
		if (function is DelegateDeclarationContext delegateDeclaration)
			return ParseDelegateDeclaration(delegateDeclaration);
		if (function is FunctionDefinitionContext functionDefinition)
			return ParseFunctionDeclaration(functionDefinition);
		if (function is GetterContext)
			return new Result.Ok(); // FIXME: This shouldn't be public - remove after struct functions are implemented
		throw new Exception($"Unknown statement type {function}");
	}
	
	/// <summary>
	/// Sets up function bodies
	/// </summary>
	public Result ParseFunctionStatementDefinition(IFunctionContext function)
	{
		if (function is CompilerFunctionContext)
			return new Result.Ok();
		if (function is ExternFunctionDeclarationContext)
			return new Result.Ok();
		if (function is DelegateDeclarationContext)
			return new Result.Ok();
		if (function is FunctionDefinitionContext functionDefinition)
			return ParseFunctionDefinition(functionDefinition);
		if (function is GetterContext)
			return new Result.Ok(); // FIXME: This shouldn't be public - remove after struct functions are implemented
		throw new Exception($"Unknown statement type {function}");
	}
}

public partial class Visitor
{
	public void VisitTypeStatement(ProgramContext program, ITypeContext type)
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
				throw new Exception($"Unknown statement type {type}");
		}
	}
	
	public void VisitFunctionStatement(ProgramContext program, IFunctionContext function)
	{
		switch (function)
		{
			case CompilerFunctionContext:
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
			case GetterContext:
				break; // FIXME: This shouldn't be public - remove after struct functions are implemented
			default:
				throw new Exception($"Unknown statement type {function}");
		}
	}
}