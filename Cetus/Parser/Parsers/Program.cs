using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<TypedType> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
}

public class IdentifiersBase : IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new Dictionary<string, TypedValue>();
	public ICollection<IFunctionContext> Functions { get; set; } = new List<IFunctionContext>();
	public ICollection<TypedType> Types { get; set; } = new List<TypedType>();
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
}

public class IdentifiersNest(IHasIdentifiers @base) : IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new NestedDictionary<string, TypedValue>(@base.Identifiers);
	public ICollection<IFunctionContext> Functions { get; set; } = new NestedCollection<IFunctionContext>(@base.Functions);
	public ICollection<TypedType> Types { get; set; } = new NestedCollection<TypedType>(@base.Types);
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
}

public static class IHasIdentifiersExtensions
{
	public static void NestFrom(this IHasIdentifiers child, IHasIdentifiers parent)
	{
		child.Identifiers = new NestedDictionary<string, TypedValue>(parent.Identifiers);
		child.Functions = new NestedCollection<IFunctionContext>(parent.Functions);
		child.Types = new NestedCollection<TypedType>(parent.Types);
	}
	
	public static List<IFunctionContext> GetFinalizedFunctions(this IHasIdentifiers program)
	{
		if (program.FinalizedFunctions is null)
		{
			program.FinalizedFunctions = program.Functions
				.Where(value => value.Pattern is not null)
				.ToList();
			program.FinalizedFunctions.Sort((a, b) => -a.Priority.CompareTo(b.Priority)); // Sort in descending order
		}
		return program.FinalizedFunctions;
	}
}

public class StructFieldContext
{
	public TypedType Type;
	public TypeIdentifier TypeIdentifier;
	public string Name;
	public int Index;
	public GetterContext Getter;
	
	public override string ToString() => $"{Type} {Name}";
}

public interface IFunctionContext
{
	public string Name { get; }
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken? Pattern { get; }
	public TypeIdentifier ReturnType { get; }
	public FunctionParametersContext Parameters { get; }
	public float Priority { get; }
}

public class FunctionParametersContext
{
	public FunctionParametersContext() { }
	
	public FunctionParametersContext(IEnumerable<(TypedType Type, string Name)> parameters, (TypedType Type, string Name)? varArg)
	{
		Parameters = parameters.Select(param => new FunctionParameterContext(new TypeIdentifier(param.Type), param.Name)).ToList();
		VarArg = varArg is null ? null : new FunctionParameterContext(new TypeIdentifier(varArg.Value.Type), varArg.Value.Name);
	}
	
	public List<FunctionParameterContext> Parameters = [];
	public FunctionParameterContext? VarArg;
	
	public int Count => Parameters.Count;
	
	public IEnumerable<FunctionParameterContext> ParamsOfCount(int count)
	{
		if (VarArg is null)
		{
			if (count != Parameters.Count)
				throw new ArgumentOutOfRangeException(nameof(count), "Count must equal the number of parameters");
			return Parameters;
		}
		else
		{
			if (count < Parameters.Count)
				throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to the number of parameters");
			return Parameters.Concat(Enumerable.Repeat(VarArg, count - Parameters.Count));
		}
	}
	
	public IEnumerable<TReturn> ZipArgs<TReturn>(ICollection<TypedValue> arguments, Func<FunctionParameterContext, TypedValue, TReturn> zip)
	{
		return ParamsOfCount(arguments.Count).Zip(arguments, zip);
	}
	
	public override string ToString() => $"({string.Join(", ", Parameters)}{(VarArg is not null ? $", {VarArg.Type}... {VarArg.Name}" : "")})";
}

public class FunctionParameterContext(TypeIdentifier type, string name)
{
	public TypeIdentifier Type => type;
	public string Name => name;
	
	public override string ToString() => $"{Type} {Name}";
}

public class FunctionParameter(TypedType type, string name)
{
	public TypedType Type => type;
	public string Name => name;
	
	public override string ToString() => $"{Type} {Name}";
}

public class ProgramContext
{
	public List<string> Libraries = [];
	public Dictionary<CompilationPhase, IHasIdentifiers> Phases = new();
	public DefineProgramCall Call { get; set; }
}