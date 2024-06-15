using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class Float : TypedValue, IToken
{
	public float Value { get; private set; }
	public TypedType Type => Visitor.FloatType;
	public LLVMValueRef LLVMValue { get; private set; }
	
	public void Parse(IHasIdentifiers context) { }
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint) { }
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		LLVMValue = LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, Value);
	}
	
	public bool Eat(string contents, ref int index)
	{
		if (char.IsDigit(contents[index]))
		{
			int i;
			bool dot = false;
			for (i = index; i < contents.Length && (char.IsDigit(contents[i]) || contents[i] == '.'); i++)
			{
				if (contents[i] == '.')
				{
					if (dot)
					{
						i--;
						break;
					}
					else
						dot = true;
				}
			}
			
			if (contents[i] == 'f')
				i++;
			else
				return false;
			
			Value = float.Parse(contents[index..(i - 1)]);
			index = i;
			return true;
		}
		
		return false;
	}
}