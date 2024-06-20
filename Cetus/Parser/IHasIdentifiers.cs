using Cetus.Parser.Types;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public IHasIdentifiers? Base { get; }
	public NestedDictionary<string, TypedValue> Identifiers { get; }
	public NestedCollection<TypedTypeFunction> Functions { get; }
	public NestedCollection<TypedType> Types { get; }
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public Program Program { get; }
}

public interface IHazIdentifiers : IHasIdentifiers
{
	public IHasIdentifiers IHasIdentifiers { get; }
	IHasIdentifiers? IHasIdentifiers.Base => IHasIdentifiers.Base;
	NestedDictionary<string, TypedValue> IHasIdentifiers.Identifiers => IHasIdentifiers.Identifiers;
	NestedCollection<TypedTypeFunction> IHasIdentifiers.Functions => IHasIdentifiers.Functions;
	NestedCollection<TypedType> IHasIdentifiers.Types => IHasIdentifiers.Types;
	Program IHasIdentifiers.Program => IHasIdentifiers.Program;
}

public class IdentifiersContext(Program program) : IHasIdentifiers
{
	public IHasIdentifiers? Base => null;
	public NestedDictionary<string, TypedValue> Identifiers { get; } = new();
	public NestedCollection<TypedTypeFunction> Functions { get; } = [];
	public NestedCollection<TypedType> Types { get; } = [];
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public Program Program => program;
}

public class IdentifiersBase(IHasIdentifiers context) : IHasIdentifiers
{
	public IHasIdentifiers? Base => null;
	public NestedDictionary<string, TypedValue> Identifiers { get; } = new(context.Identifiers);
	public NestedCollection<TypedTypeFunction> Functions { get; } = new(context.Functions);
	public NestedCollection<TypedType> Types { get; } = new(context.Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public Program Program => context.Program;
}

public class IdentifiersNest(IHasIdentifiers @base, CompilationPhase phase) : IHasIdentifiers
{
	public IHasIdentifiers Base => @base;
	public NestedDictionary<string, TypedValue> Identifiers { get; } = new(@base.Identifiers, @base.Program.Contexts[phase].Identifiers);
	public NestedCollection<TypedTypeFunction> Functions { get; } = new(@base.Functions, @base.Program.Contexts[phase].Functions);
	public NestedCollection<TypedType> Types { get; } = new(@base.Types, @base.Program.Contexts[phase].Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public Program Program => @base.Program;
}

public static class IHasIdentifiersExtensions
{
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
	
	public static List<TypedTypeWithPattern> GetFinalizedTypes(this IHasIdentifiers program)
	{
		if (program.FinalizedTypes is null)
		{
			program.FinalizedTypes = program.Types
				.OfType<TypedTypeWithPattern>()
				.ToList();
			program.FinalizedTypes.Sort((a, b) => -a.Priority.CompareTo(b.Priority)); // Sort in descending order
		}
		return program.FinalizedTypes;
	}
}