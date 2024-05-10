using Cetus.Parser.Values;

namespace Cetus.Parser.Contexts;

public class ProgramContext : IHasIdentifiers
{
	public Dictionary<string, TypedValue> Identifiers { get; } = new();
}