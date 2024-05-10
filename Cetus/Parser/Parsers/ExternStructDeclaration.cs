using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseExternStructDeclaration(ProgramContext context)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			lexer.Eat<Struct>() &&
			lexer.Eat(out Word? structName) &&
			lexer.Eat<Semicolon>())
		{
			string name = structName.TokenText;
			LLVMTypeRef structValue = LLVMContextRef.Global.CreateNamedStruct(name);
			TypedValue @struct = new TypedValueType(new TypedTypeStruct(structValue));
			context.Identifiers.Add(name, @struct);
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected extern struct declaration", lexer.Line, lexer.Column);
		}
	}
}