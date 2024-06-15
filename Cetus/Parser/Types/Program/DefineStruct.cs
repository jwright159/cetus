using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineStruct() : TypedTypeFunctionBase("DefineStruct", Visitor.VoidType, new FunctionParameters([(Visitor.CompilerStringType, "name"), (Visitor.AnyFunctionType.List(), "functions"), (Visitor.TypeIdentifierType.List(), "fieldTypes"), (Visitor.CompilerStringType.List(), "fieldNames")], null))
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineStructCall(
			context,
			((TypedValueCompiler<string>)args["name"]).CompilerValue,
			((TypedValueCompiler<List<TypedValueCompiler<TypeIdentifier>>>)args["fieldTypes"]).CompilerValue.Zip(((TypedValueCompiler<List<TypedValueCompiler<string>>>)args["fieldNames"]).CompilerValue, (type, name) => (type.CompilerValue, name.CompilerValue)).ToList());
	}
}

public class DefineStructCall(IHasIdentifiers parent, string name, List<(TypeIdentifier Type, string Name)> fields) : TypedValue, TypedType, IHasIdentifiers
{
	public string Name => name;
	public TypedType Type { get; } = new TypedTypeStruct(LLVMContextRef.Global.CreateNamedStruct(name));
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	public LLVMTypeRef LLVMType => Type.LLVMType;
	public List<StructFieldContext> Fields = [];
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new NestedDictionary<string, TypedValue>(parent.Identifiers);
	public ICollection<TypedTypeFunction> Functions { get; set; } = new NestedCollection<TypedTypeFunction>(parent.Functions);
	public ICollection<TypedType> Types { get; set; } = new NestedCollection<TypedType>(parent.Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public Dictionary<TypedTypeFunction, LateCompilerFunctionContext> FunctionGetters = new();
	public Dictionary<TypedTypeFunction, LateCompilerFunctionContext> FunctionCallers = new();
	
	public void Parse(IHasIdentifiers context)
	{
		context.Types.Add(this);
		context.Identifiers.Add(Name, new TypedValueType(Type));
		
		foreach ((TypeIdentifier fieldType, string fieldName) in fields)
		{
			StructFieldContext field = new();
			field.TypeIdentifier = fieldType;
			field.Name = fieldName;
			field.Index = Fields.Count;
			field.Getter = new GetterContext(this, field);
			
			Fields.Add(field);
			Functions.Add(field.Getter);
		}
		
		NestedCollection<TypedTypeFunction> functions = (NestedCollection<TypedTypeFunction>)Functions;
		foreach (TypedTypeFunction function in functions.ThisList)
		{
			{
				LateCompilerFunctionContext getterFunction = new(
					new TypeIdentifier($"{Name}.{function.Name}"),
					$"{Name}.Get_{function.Name}",
					0,
					new TokenString([new ParameterValueToken("type"), new LiteralToken("."), new LiteralToken(function.Name)]),
					new FunctionParameters { Parameters = [new FunctionParameter(new TypeIdentifier("Type", new TypeIdentifier(Name)), "type")] });
				FunctionGetters.Add(function, getterFunction);
				context.Functions.Add(getterFunction);
			}
			
			if (function.Parameters.Parameters[0].Type.Name == Name)
			{
				LateCompilerFunctionContext callerFunction = new(
					new TypeIdentifier($"{Name}.{function.Name}"),
					$"{Name}.Call_{function.Name}",
					0,
					new TokenString([new ParameterValueToken("this"), new LiteralToken("."), new LiteralToken(function.Name)]),
					new FunctionParameters { Parameters = [new FunctionParameter(new TypeIdentifier(Name), "this")] });
				FunctionCallers.Add(function, callerFunction);
				context.Functions.Add(callerFunction);
			}
		}
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		foreach (StructFieldContext field in Fields)
		{
			field.Type = context.Types.First(type => type.Name == field.TypeIdentifier.Name);
		}
		
		foreach (TypedTypeFunction function in Functions)
		{
			// convert returns/params
			// also parse + transform closures
			
			
			if (FunctionGetters.TryGetValue(function, out LateCompilerFunctionContext? getterFunction))
				getterFunction.Type = new MethodGetter(this, function);
			
			if (FunctionCallers.TryGetValue(function, out LateCompilerFunctionContext? callerFunction))
				callerFunction.Type = new MethodCaller(this, function);
		}
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		Type.LLVMType.StructSetBody(Fields.Select(field => field.Type.LLVMType).ToArray(), false);
	}
}