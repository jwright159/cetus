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

public class StructDefinitionContext : ITypeContext
{
	public string Name { get; set; }
	public TypedType Type { get; set; }
	public int LexerStartIndex { get; set; }
	public List<StructFieldContext> Fields;
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
			program.Types.Add(structDefinition);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
	
	public Result ParseStructDefinition(ProgramContext program, StructDefinitionContext structDefinition)
	{
		lexer.Index = structDefinition.LexerStartIndex;
		if (
			lexer.Eat<Word>() &&
			ParseStructBlock(out List<StructFieldContext> fields) is Result.Passable structBlockResult)
		{
			structDefinition.Fields = fields;
			LLVMTypeRef structValue = LLVMContextRef.Global.CreateNamedStruct(structDefinition.Name);
            TypedTypeStruct @struct = new(structValue);
            structDefinition.Type = @struct;
			
			foreach ((int i, StructFieldContext field) in fields.Enumerate())
			{
				field.Type = program.Types.First(pair => pair.Name == field.TypeIdentifier.Name).Type;
				TypedTypeFunctionGetter getter = new(structDefinition, field, i);
				program.Functions.Add(new CompilerFunctionContext(getter, [new ParameterExpressionToken(0), new LiteralToken("."), new LiteralToken(field.Name)]), new TypedValueType(getter));
			}
			
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
	public void VisitStructDefinition(ProgramContext program, StructDefinitionContext structDefinition)
	{
		structDefinition.Type.LLVMType.StructSetBody(structDefinition.Fields.Select(field => VisitTypeIdentifier(program, field.TypeIdentifier).LLVMType).ToArray(), false);
		program.Identifiers.Add(structDefinition.Name, new TypedValueType(structDefinition.Type));
	}
}