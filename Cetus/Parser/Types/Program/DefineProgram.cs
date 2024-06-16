using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineProgram : TypedTypeFunctionBase
{
	public override string Name => "DefineProgram";
	public override IToken Pattern => new TokenSplit(new SOFToken(), new LiteralToken(";"), new EOFToken(), new ParameterStatementToken("statements"));
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(Visitor.AnyFunctionCall.List(), "statements")], null);
	public override float Priority => 1000;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		List<FunctionCall> statements = ((TypedValueCompiler<List<FunctionCall>>)args["statements"]).CompilerValue;
		Console.WriteLine("STATEMENTS: [\n\t" + string.Join(",\n", statements).Replace("\n", "\n\t") + "\n]");
		return new DefineProgramCall(
			statements.Select(statement => statement.Call(context)).ToList());
	}
}

public class DefineProgramCall(List<TypedValue> statements) : TypedValue
{
	public TypedType Type => Visitor.VoidType;
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	
	public void Parse(IHasIdentifiers context)
	{
		statements.ForEach(statement =>
		{
			statement.Parse(context);
			if (statement is DefineFunctionCall function)
				context.Identifiers.Add(function.Name, statement);
		});
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		statements.ForEach(statement => statement.Transform(context, null));
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		statements.ForEach(statement => statement.Visit(context, null, visitor));
	}
}