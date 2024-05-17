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
	public bool ParseExternStructDeclarationFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			lexer.Eat<Struct>() &&
			lexer.Eat(out Word? structName))
		{
			ExternStructDeclarationContext externStructDeclaration = new();
			externStructDeclaration.Name = structName.TokenText;
			externStructDeclaration.LexerStartIndex = startIndex;
			program.Types.Add(externStructDeclaration);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
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
	public void VisitExternStructDeclaration(ProgramContext program, ExternStructDeclarationContext externStructDeclaration)
	{
		program.Identifiers.Add(externStructDeclaration.Name, new TypedValueType(externStructDeclaration.Type));
	}
}