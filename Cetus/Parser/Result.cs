namespace Cetus.Parser;

public interface Result
{
	/// <summary>
	/// Represents a whole or partial successful value.
	/// </summary>
	public interface Passable : Result;
	
	/// <summary>
	/// Represents any kind of failure.
	/// </summary>
	public interface Failure : Result;
	
	/// <summary>
	/// Represents a completely successful value.
	/// </summary>
	public class Ok : Passable
	{
		public override string ToString() => "Ok";
	}
	
	/// <summary>
	/// Represents a completely failed value. These values are typically walked back in the parser, showing that the
	/// attempted parsing did not happen at all.
	/// </summary>
	public class TokenRuleFailed(string message, int line, int column) : Failure
	{
		public override string ToString() => $"TokenRule failed at ({line}, {column}): {message}";
	}
	
	/// <summary>
	/// Represents a partially failed value. These values are typically followed through to a final token, returning a
	/// partial and potentially usable value.
	/// </summary>
	public class ComplexRuleFailed : Passable, Failure
	{
		private readonly string message;
		private readonly Result?[] results;
		
		public ComplexRuleFailed(string message, params Result?[] results)
		{
			this.message = message;
			this.results = results;
			
			if (this.results.Length == 0)
				throw new ArgumentException("ComplexRuleFailed must have at least one result", nameof(results));
		}
		
		public override string ToString() => $"ComplexRule failed: {message}\n\t{string.Join("\n", results.Where(result => result is Failure).Select(result => result!.ToString()!)).Replace("\n", "\n\t")}";
	}
	
	public static Result WrapPassable(string message, params Result?[] results)
	{
		return results.All(result => result is null or Ok) ? new Ok() : new ComplexRuleFailed(message, results.Where(result => result is Failure).ToArray());
	}
	
	public static ComplexRuleFailed ComplexTokenRuleFailed(string message, int line, int column)
	{
		return new ComplexRuleFailed(message, new TokenRuleFailed(message, line, column));
	}
}