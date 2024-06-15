using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class String : TypedValue, IToken
{
	public string Value { get; private set; }
	public TypedType Type => Visitor.StringType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		LLVMValue = builder.BuildGlobalStringPtr(Value, Value.Length == 0 ? "emptyString" : Value);
	}
	
	public bool Eat(string contents, ref int index)
	{
		if (contents[index] == '"')
		{
			int i;
			for (i = index + 1; i < contents.Length && contents[i] != '"'; i++)
			{
				if (contents[i] == '\\')
					i++;
			}
			
			if (contents[i] == '"')
				i++;
			else
				return false;
			
			Value = System.Text.RegularExpressions.Regex.Unescape(contents[(index + 1)..(i - 1)]);
			index = i;
			return true;
		}
		
		return false;
	}
}