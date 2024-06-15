using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public abstract class Integer : TypedValue, IToken
{
	public int Value { get; protected set; }
	public TypedType Type => Visitor.IntType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		LLVMValue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Value, true);
	}
	
	public abstract bool Eat(string contents, ref int index);
}

public class DecimalInteger : Integer
{
	public override bool Eat(string contents, ref int index)
	{
		if (char.IsDigit(contents[index]))
		{
			int i = index;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			Value = int.Parse(contents[index..i]);
			index = i;
			return true;
		}
		
		return false;
	}
}

public class HexInteger : Integer
{
	public override bool Eat(string contents, ref int index)
	{
		if (contents.Length > index + 2 && contents[index..(index+2)] == "0x")
		{
			int i = index + 2;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			Value = int.Parse(contents[(index + 1)..i]);
			index = i;
			return true;
		}
		
		return false;
	}
}