using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface ITypeContext
{
	public string Name { get; }
	public TypedType Type { get; }
}

public class StructDefinitionContext : ITypeContext, IHasIdentifiers
{
	public string Name { get; set; }
	public TypedType Type { get; set; }
	public int LexerStartIndex { get; set; }
	public List<IStructStatementContext> Statements;
	public List<StructFieldContext> Fields = [];
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
}

public partial class Parser
{
	public bool ParseStructDefinitionFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat(out Word? structName) &&
			lexer.EatMatches<LeftBrace, RightBrace>())
		{
			StructDefinitionContext structDefinition = new();
			structDefinition.Name = structName.TokenText;
			structDefinition.LexerStartIndex = startIndex;
			structDefinition.NestFrom(program);
			program.Types.Add(structDefinition);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseStructDefinition(StructDefinitionContext structDefinition)
	{
		lexer.Index = structDefinition.LexerStartIndex;
		if (
			lexer.Eat<Word>() &&
			ParseStructBlock(structDefinition, out List<IStructStatementContext> statements) is Result.Passable structBlockResult)
		{
			structDefinition.Statements = statements;
			return Result.WrapPassable($"Invalid struct definition for '{structDefinition.Name}'", structBlockResult);
		}
		else
		{
			return new Result.TokenRuleFailed($"Expected struct definition for '{structDefinition.Name}'", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitStructDefinition(IHasIdentifiers program, StructDefinitionContext structDefinition)
	{
		LLVMTypeRef structValue = LLVMContextRef.Global.CreateNamedStruct(structDefinition.Name);
		TypedTypeStruct @struct = new(structValue);
		@struct.LLVMType.StructSetBody(structDefinition.Statements.OfType<StructFieldContext>().Select(field => VisitTypeIdentifier(program, field.TypeIdentifier).LLVMType).ToArray(), false);
		structDefinition.Type = @struct;
		program.Identifiers.Add(structDefinition.Name, new TypedValueType(structDefinition.Type));
		VisitStructBlock(structDefinition, structDefinition.Statements);
	}
}