using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public interface ITypeContext
{
	public string Name { get; }
	public TypedType Type { get; }
}

public class StructDefinitionContext : ITypeContext, IHasIdentifiers
{
	public string Name { get; set; }
	public TypedType Type { get; set; }
	public List<StructFieldContext> Fields = [];
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
	public Dictionary<IFunctionContext, LateCompilerFunctionContext> FunctionGetters = new();
	public Dictionary<IFunctionContext, LateCompilerFunctionContext> FunctionCallers = new();
}

public partial class Parser
{
	public Result ParseStructDefinitionFirstPass(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat(out Word? structName) &&
			lexer.EatMatches<LeftBrace, RightBrace>())
		{
			StructDefinitionContext @struct = new();
			@struct.Name = structName.Value;
			@struct.NestFrom(program);
			program.Types.Add(@struct);
			
			lexer.Index = startIndex;
			lexer.Eat<Word>();
			Result structBlockResult = ParseStructBlockFirstPass(@struct);
			
			NestedCollection<IFunctionContext> functions = (NestedCollection<IFunctionContext>)@struct.Functions;
			foreach (IFunctionContext function in functions.ThisList)
			{
				{
					LateCompilerFunctionContext getterFunction = new(
						new TypeIdentifierContext { Name = $"{@struct.Name}.{function.Name}" },
						$"{@struct.Name}.Get_{function.Name}",
						0,
						[new ParameterValueToken(0), new LiteralToken("."), new LiteralToken(function.Name)],
						new FunctionParametersContext { Parameters = [new FunctionParameterContext(new TypeIdentifierContext { Name = "Type", InnerType = new TypeIdentifierContext { Name = @struct.Name } }, "type")] });
					@struct.FunctionGetters.Add(function, getterFunction);
					functions.SuperList.Add(getterFunction);
				}
				
				if (function.ParameterContexts.Parameters[0].Type.Name == @struct.Name)
				{
					LateCompilerFunctionContext callerFunction = new(
						new TypeIdentifierContext { Name = $"{@struct.Name}.{function.Name}" },
						$"{@struct.Name}.Call_{function.Name}",
						0,
						[new ParameterValueToken(0), new LiteralToken("."), new LiteralToken(function.Name)],
						new FunctionParametersContext { Parameters = [new FunctionParameterContext(new TypeIdentifierContext { Name = @struct.Name }, "this")] });
					@struct.FunctionCallers.Add(function, callerFunction);
					functions.SuperList.Add(callerFunction);
				}
			}
			
			foreach (StructFieldContext field in @struct.Fields)
				@struct.Functions.Add(field.Getter);
			
			return Result.WrapPassable("Invalid struct definition", structBlockResult);
		}
		lexer.Index = startIndex;
		return new Result.TokenRuleFailed("Expected struct definition", lexer.Line, lexer.Column);
	}
	
	public Result ParseStructDefinition(StructDefinitionContext @struct)
	{
		List<Result> results = [];
		foreach (IFunctionContext function in ((NestedCollection<IFunctionContext>)@struct.Functions).ThisList)
			if (ParseFunctionStatementDefinition(function) is Result.Failure result)
				results.Add(result);
		return Result.WrapPassable("Invalid struct definition", results.ToArray());
	}
}

public partial class Visitor
{
	public void VisitStructDefinition(IHasIdentifiers program, StructDefinitionContext @struct)
	{
		foreach (StructFieldContext field in @struct.Fields)
			VisitStructField(@struct, field);
		
		LLVMTypeRef structValue = LLVMContextRef.Global.CreateNamedStruct(@struct.Name);
		TypedTypeStruct structType = new(structValue);
		structType.LLVMType.StructSetBody(@struct.Fields.Select(field => field.Type.LLVMType).ToArray(), false);
		@struct.Type = structType;
		program.Identifiers.Add(@struct.Name, new TypedValueType(@struct.Type));
		
		NestedCollection<IFunctionContext> functions = (NestedCollection<IFunctionContext>)@struct.Functions;
		foreach (IFunctionContext function in functions.ThisList)
		{
			VisitFunctionStatement(@struct, function);
			
			TypedTypeFunction returnFunction = (TypedTypeFunction)function.Type;
			
			if (@struct.FunctionGetters.TryGetValue(function, out LateCompilerFunctionContext? getterFunction))
				getterFunction.Type = new MethodGetter(@struct, returnFunction);
			
			if (@struct.FunctionCallers.TryGetValue(function, out LateCompilerFunctionContext? callerFunction))
				callerFunction.Type = new MethodCaller(@struct, returnFunction);
		}
	}
}