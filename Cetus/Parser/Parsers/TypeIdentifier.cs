using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class TypeIdentifier : TypedValue
{
	public string Name { get; }
	public TypedType? Type { get; private set; }
	public LLVMValueRef LLVMValue { get; }
	public TypeIdentifier? InnerType { get; }
	
	public TypeIdentifier(string name, TypeIdentifier? innerType = null)
	{
		Name = name;
		InnerType = innerType;
	}
	
	public TypeIdentifier(TypedType type)
	{
		Name = type.Name;
		Type = type;
		InnerType = type.InnerType is not null ? new TypeIdentifier(type.InnerType) : null;
	}
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		InnerType?.Transform(context, null);
		
		if (Type is not null)
			return;
		
		if (Name == "Closure")
		{
			DefinedFunctionCall functionType = new("block", this, InnerType ?? new TypeIdentifier(Visitor.VoidType), new FunctionParameters([(new TypedTypePointer(new TypedTypeChar()), "data")], null));
			TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
			Type = new TypedTypeClosurePointer(closureStructType, functionType);
		}
		else if (Name == "CompilerClosure")
		{
			Type = new TypedTypeCompilerClosure(InnerType?.Type ?? Visitor.VoidType);
		}
		else
		{
			if (!context.Identifiers.TryGetValue(Name, out TypedValue? value))
				throw new Exception($"Identifier '{Name}' not found");
			
			if (value is not TypedValueType)
				throw new Exception($"'{Name}' is not a type");
			
			Type = value.Type;
		}
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
	
	public TypeIdentifier Pointer() => new("Pointer", this);
	
	public override string ToString() => $"{Name}{(InnerType is not null ? $"<{InnerType}>" : "")}";
}