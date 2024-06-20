using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface TypeIdentifier : TypedValue
{
	LLVMValueRef TypedValue.LLVMValue => throw new Exception("TypeIdentifier does not have an LLVMValue");
}

public static class TypeIdentifierExtensions
{
	public static TypeIdentifier Pointer(this TypeIdentifier identifier) => TypeIdentifierCall.Pointer(identifier);
}

public class TypeIdentifierBase(TypedType type) : TypeIdentifier
{
	public TypedType Type => type;
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
}

public class TypeIdentifierCall(IHasIdentifiers parent, int order) : TypeIdentifier, IToken
{
	public TypedType? Type { get; private set; }
	public TypedTypeWithPattern MatchedType { get; private set; }
	public TypeArgs Arguments { get; private set; }
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		foreach (TypedTypeWithPattern type in parent.GetFinalizedTypes().Skip(order))
		{
			order++;
			IToken token = type.Pattern;
			TypeArgs arguments = new(type.TypeParameters);
			
			Result result = lexer.Eat(token.Contextualize(parent, arguments, order));
			if (result is not Result.Passable)
			{
				lexer.Index = startIndex;
				continue;
			}
			
			Arguments = arguments;
			MatchedType = type;
			
			return result;
		}
		
		return new Result.TokenRuleFailed("Expected function call", lexer, startIndex);
	}
	
	public void Parse(IHasIdentifiers context)
	{
		Arguments.Parse(context);
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		if (Type is not null)
			return;
		
		Arguments.Transform(context);
		Type = MatchedType.Call(context, Arguments);
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		Arguments.Visit(context, visitor);
	}
	
	public static TypeIdentifierCall Pointer(TypeIdentifier identifier)
	{
		TypeIdentifierCall call = new(null, 0);
		call.MatchedType = new TypedTypePointer();
		call.Arguments = new TypeArgs(new TypeParameters(["innerType"]));
		call.Arguments["innerType"] = identifier;
		return call;
	}
	
	public override string ToString() => $"{MatchedType.Name}{Arguments}";
}

public class TypeIdentifierName(string name) : TypeIdentifier
{
	public string Name => name;
	public TypedType? Type { get; private set; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		if (Type is not null)
			return;
		
		if (Name == "Closure")
		{
			DefinedFunctionCall functionType = new("block", this, Visitor.VoidType.Id(), new FunctionParameters([(new TypedTypePointer(new TypedTypeChar()), "data")], null));
			TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
			Type = new TypedTypeClosurePointer(closureStructType, functionType);
		}
		else if (Name == "CompilerClosure")
		{
			Type = new TypedTypeCompilerClosure(Visitor.VoidType);
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
	
	public override string ToString() => $"{Name}";
}