using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
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
	
	public Result ParseTypeStatementDeclaration(ProgramContext program, ITypeContext type)
	{
		if (type is CompilerTypeContext)
			return new Result.Ok();
		if (type is ExternStructDeclarationContext)
			return new Result.Ok();
		if (type is StructDefinitionContext)
			return new Result.Ok();
		throw new Exception($"Unknown statement type {type}");
	}
	
	public Result ParseTypeStatementDefinition(ProgramContext program, ITypeContext type)
	{
		if (type is CompilerTypeContext)
			return new Result.Ok();
		if (type is ExternStructDeclarationContext externStructDeclaration)
			return ParseExternStructDefinition(externStructDeclaration);
		if (type is StructDefinitionContext structDefinition)
			return ParseStructDefinition(program, structDefinition);
		throw new Exception($"Unknown statement type {type}");
	}
	
	public Result ParseFunctionStatementDeclaration(ProgramContext program, IFunctionContext function)
	{
		if (function is CompilerFunctionContext)
			return new Result.Ok();
		if (function is ExternFunctionDeclarationContext externFunctionDeclaration)
			return ParseExternFunctionDeclaration(externFunctionDeclaration);
		if (function is DelegateDeclarationContext delegateDeclaration)
			return ParseDelegateDeclaration(delegateDeclaration);
		if (function is FunctionDefinitionContext functionDefinition)
			return ParseFunctionDeclaration(program, functionDefinition);
		throw new Exception($"Unknown statement type {function}");
	}
	
	public Result ParseFunctionStatementDefinition(ProgramContext program, IFunctionContext function)
	{
		if (function is CompilerFunctionContext)
			return new Result.Ok();
		if (function is ExternFunctionDeclarationContext)
			return new Result.Ok();
		if (function is DelegateDeclarationContext)
			return new Result.Ok();
		if (function is FunctionDefinitionContext functionDefinition)
			return ParseFunctionDefinition(program, functionDefinition);
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
			default:
				throw new Exception($"Unknown statement type {function}");
		}
	}
}