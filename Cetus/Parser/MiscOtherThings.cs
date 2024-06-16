using Cetus.Parser.Types;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<TypedTypeFunction> Functions { get; set; }
	public ICollection<TypedType> Types { get; set; }
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
}

public class IdentifiersBase : IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new Dictionary<string, TypedValue>();
	public ICollection<TypedTypeFunction> Functions { get; set; } = new List<TypedTypeFunction>();
	public ICollection<TypedType> Types { get; set; } = new List<TypedType>();
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
}

public class IdentifiersNest(IHasIdentifiers @base) : IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new NestedDictionary<string, TypedValue>(@base.Identifiers);
	public ICollection<TypedTypeFunction> Functions { get; set; } = new NestedCollection<TypedTypeFunction>(@base.Functions);
	public ICollection<TypedType> Types { get; set; } = new NestedCollection<TypedType>(@base.Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
}

public static class IHasIdentifiersExtensions
{
	public static void NestFrom(this IHasIdentifiers child, IHasIdentifiers parent)
	{
		child.Identifiers = new NestedDictionary<string, TypedValue>(parent.Identifiers);
		child.Functions = new NestedCollection<TypedTypeFunction>(parent.Functions);
		child.Types = new NestedCollection<TypedType>(parent.Types);
	}
	
	public static List<TypedTypeFunction> GetFinalizedFunctions(this IHasIdentifiers program)
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
	public Getter Getter;
	
	public override string ToString() => $"{Type} {Name}";
}

public class FunctionParameters
{
	public FunctionParameters(IEnumerable<FunctionParameter> parameters, FunctionParameter? varArg)
	{
		Parameters = parameters.ToList();
		VarArg = varArg;
	}
	
	public FunctionParameters(IEnumerable<(TypedType Type, string Name)> parameters, (TypedType Type, string Name)? varArg)
	{
		Parameters = parameters.Select(param => new FunctionParameter(new TypeIdentifier(param.Type), param.Name)).ToList();
		VarArg = varArg is null ? null : new FunctionParameter(new TypeIdentifier(varArg.Value.Type), varArg.Value.Name);
	}
	
	public List<FunctionParameter> Parameters = [];
	public FunctionParameter? VarArg;
	
	public int Count => Parameters.Count;
	
	public IEnumerable<FunctionParameter> ParamsOfCount(int count)
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
	
	public IEnumerable<TReturn> ZipArgs<TReturn>(ICollection<TypedValue> arguments, Func<FunctionParameter, TypedValue, TReturn> zip)
	{
		return ParamsOfCount(arguments.Count).Zip(arguments, zip);
	}
	
	public IEnumerable<(TypedType Type, string Name)> TupleParams => Parameters.Select(param => (param.Type.Type, param.Name));
	
	public override string ToString() => $"({string.Join(", ", Parameters)}{(VarArg is not null ? $", {VarArg.Type}... {VarArg.Name}" : "")})";
}

public class FunctionParameter(TypeIdentifier type, string name)
{
	public TypeIdentifier Type => type;
	public string Name => name;
	
	public override string ToString() => $"{Type} {Name}";
}

public class ProgramContext
{
	public List<string> Libraries = [];
	public Dictionary<CompilationPhase, IHasIdentifiers> Phases = new();
	public DefineProgramCall Call { get; set; }
}