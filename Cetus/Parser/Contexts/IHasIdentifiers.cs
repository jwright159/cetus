using Cetus.Parser.Values;

namespace Cetus.Parser.Contexts;

public interface IHasIdentifiers
{
	public Dictionary<string, TypedValue> Identifiers { get; }
}