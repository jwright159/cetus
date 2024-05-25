﻿using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class TypeIdentifierContext
{
	public string Name;
	public TypeIdentifierContext? InnerType;
	public int PointerDepth;
	
	public TypeIdentifierContext Pointer()
	{
		TypeIdentifierContext result = new();
		result.Name = Name;
		result.InnerType = InnerType;
		result.PointerDepth = PointerDepth + 1;
		return result;
	}
	
	public override string ToString() => $"{Name}{(InnerType is not null ? $"<{InnerType}>" : "")}{new string('*', PointerDepth)}";
}

public partial class Parser
{
	public Result ParseTypeIdentifier(out TypeIdentifierContext type)
	{
		if (lexer.Eat(out Word? typeName))
		{
			List<Result> results = [];
			type = new TypeIdentifierContext();
			type.Name = typeName.Value;
			
			if (lexer.Eat<LeftTriangle>())
			{
				results.Add(ParseTypeIdentifier(out TypeIdentifierContext innerType));
				type.InnerType = innerType;
				if (lexer.SkipToMatches<RightTriangle>(out int line, out int column))
					results.Add(new Result.TokenRuleFailed("Expected '>'", line, column));
			}
			
			while (lexer.Eat<Pointer>())
				type.PointerDepth++;
			
			return Result.WrapPassable("Invalid type identifier", results.ToArray());
		}
		else
		{
			type = null;
			return new Result.TokenRuleFailed("Expected type identifier", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedType VisitTypeIdentifier(IHasIdentifiers program, TypeIdentifierContext type)
	{
		TypedType result;
		if (type.Name == "Closure")
		{
			TypedType? innerType = type.InnerType is not null ? VisitTypeIdentifier(program, type.InnerType) : null;
			FunctionCall functionType = new("block", innerType ?? VoidType, [new TypedTypePointer(new TypedTypeChar())], null);
			TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
			result = new TypedTypeClosurePointer(closureStructType, functionType);
		}
		else if (type.Name == "CompilerClosure")
		{
			TypedType? innerType = type.InnerType is not null ? VisitTypeIdentifier(program, type.InnerType) : null;
			result = new TypedTypeCompilerClosure(innerType ?? VoidType);
		}
		else
		{
			if (!program.Identifiers.TryGetValue(type.Name, out TypedValue? value))
				throw new Exception($"Identifier '{type.Name}' not found");
			
			if (value is not TypedValueType)
				throw new Exception($"'{type.Name}' is not a type");
			
			result = value.Type;
		}
		for (int i = 0; i < type.PointerDepth; i++)
			result = new TypedTypePointer(result);
		return result;
	}
}