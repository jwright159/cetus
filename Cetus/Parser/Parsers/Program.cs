using Cetus.Parser.Contexts;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseProgram(ProgramContext context)
	{
		List<Result> results = [];
		while (ParseProgramStatement(context) is { } statementResult)
		{
			if (statementResult is Result.Failure)
				results.Add(statementResult);
			if (statementResult is Result.TokenRuleFailed)
				break;
		}
		return Result.WrapPassable("Invalid program", results.ToArray());
	}
}