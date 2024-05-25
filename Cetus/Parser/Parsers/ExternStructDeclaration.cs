using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ExternStructDeclarationContext : ITypeContext
{
	public string Name { get; set; }
	public TypedType Type { get; set; }
	public int LexerStartIndex { get; set; }
}

public partial class Parser
{
	public Result ParseExternStructDeclarationFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			lexer.Eat(out Word? structName))
		{
			ExternStructDeclarationContext externStructDeclaration = new();
			externStructDeclaration.Name = structName.Value;
			externStructDeclaration.LexerStartIndex = startIndex;
			program.Types.Add(externStructDeclaration);
			return new Result.Ok();
		}
		lexer.Index = startIndex;
		return new Result.TokenRuleFailed("Expected external struct declaration", lexer.Line, lexer.Column);
	}
	
	public Result ParseExternStructDefinition(ExternStructDeclarationContext externStructDeclaration)
	{
		LLVMTypeRef structValue = LLVMContextRef.Global.CreateNamedStruct(externStructDeclaration.Name);
		TypedTypeStruct @struct = new(structValue);
		externStructDeclaration.Type = @struct;
		return new Result.Ok();
	}
}

public partial class Visitor
{
	public void VisitExternStructDeclaration(IHasIdentifiers program, ExternStructDeclarationContext externStructDeclaration)
	{
		program.Identifiers.Add(externStructDeclaration.Name, new TypedValueType(externStructDeclaration.Type));
	}
}