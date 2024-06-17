using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class TypeIdentifier : TypedValue, IToken
{
	public string Name { get; private set; }
	public TypedType? Type { get; private set; }
	public LLVMValueRef LLVMValue => throw new Exception("TypeIdentifier does not have an LLVMValue");
	public TypeIdentifier? InnerType { get; private set; }
	
	public TypeIdentifier() { }
	
	public TypeIdentifier(string name, TypeIdentifier? innerType = null)
	{
		Name = name;
		InnerType = innerType;
	}
	
	public TypeIdentifier(TypedType type) : this(type.Name, type is TypedTypeWithInnerType { InnerType: not null } withInnerType ? new TypeIdentifier(withInnerType.InnerType) : null)
	{
		Type = type;
	}
	
	public Result Eat(Lexer lexer)
	{
		List<Result> results = [];
		
		Result nameResult = lexer.Eat(out Word name);
		results.Add(nameResult);
		if (nameResult is Result.Passable)
			Name = name.Value;
		
		// This sucks! Implement type patterns
		if (lexer.Eat(new LiteralToken("[")) is Result.Passable)
		{
			results.Add(lexer.Eat(out TypeIdentifier innerType));
			InnerType = innerType;
			results.Add(lexer.Eat(new LiteralToken("]")));
		}
		
		return Result.WrapPassable("Invalid type identifier", results.ToArray());
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
		
		if (InnerType is not null)
			((TypedTypeWithInnerType)Type).InnerType = InnerType?.Type;
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
	
	public TypeIdentifier Pointer() => new("Pointer", this);
	
	public override string ToString() => $"{Name}{(InnerType is not null ? $"<{InnerType}>" : "")}";
}