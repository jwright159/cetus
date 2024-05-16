using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ClosureContext : IValueContext, IHasIdentifiers
{
	public List<IFunctionStatementContext> Statements;
	public ProgramContext Program { get; set; }
	public Dictionary<string, TypedValue> Identifiers { get; set; }
}

public partial class Parser
{
	public Result ParseClosure(IHasIdentifiers program, out ClosureContext closure)
	{
		int startIndex = lexer.Index;
		if (ParseFunctionBlock(program, out List<IFunctionStatementContext> statements) is Result.Passable functionBlockResult)
		{
			closure = new ClosureContext();
			closure.Statements = statements;
			closure.Program = program.Program;
			return Result.WrapPassable("Expected closure", functionBlockResult);
		}
		lexer.Index = startIndex;
		closure = null;
		return new Result.TokenRuleFailed("Expected closure", lexer.Line, lexer.Column);
	}
}

public partial class Visitor
{
	public TypedValue VisitClosure(IHasIdentifiers program, ClosureContext closure, TypedType? typeHint)
	{
		if (typeHint is not TypedTypeClosurePointer and not TypedTypeCompilerClosure)
			throw new Exception("Expected closure");
		
		if (typeHint is TypedTypeCompilerClosure compilerClosureType)
		{
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
			TypedValueCompilerClosure compilerClosure = new(compilerClosureType, block);
			builder.PositionAtEnd(block);

			closure.Identifiers = new Dictionary<string, TypedValue>(program.Identifiers);
			closure.Identifiers["Return"] = new TypedValueType(new TypedTypeFunctionReturnCompilerClosure(compilerClosure));
			closure.Identifiers["ReturnVoid"] = new TypedValueType(new TypedTypeFunctionReturnVoidCompilerClosure(compilerClosure));
			
			VisitFunctionBlock(closure, closure.Statements);
			
			builder.PositionAtEnd(originalBlock);
			
			return compilerClosure;
		}
		else
		{
			Dictionary<string, TypedValue> uniqueClosureIdentifiers = program.Identifiers.Except(program.Program.Identifiers).ToDictionary();
			TypedTypeStruct closureEnvType = new(LLVMTypeRef.CreateStruct(uniqueClosureIdentifiers.Values.Select(type => type.Type.LLVMType).ToArray(), false));
			
			TypedTypeFunction functionType = (typeHint as TypedTypeClosurePointer)?.BlockType ?? new TypedTypeFunction("closure_block", ((TypedTypeCompilerClosure)typeHint).ReturnType, [new TypedTypePointer(new TypedTypeChar())], null);
			LLVMValueRef function = module.AddFunction("closure_block", functionType.LLVMType);
			function.Linkage = LLVMLinkage.LLVMInternalLinkage;
			
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			builder.PositionAtEnd(function.AppendBasicBlock("entry"));
			
			// Unpack the closure environment in the function
			{ 
				closure.Identifiers = new Dictionary<string, TypedValue>(program.Identifiers);
				LLVMValueRef closureEnvPtr = function.GetParam(0);
				int paramIndex = 0;
				foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
				{
					LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnvPtr, (uint)paramIndex++, name);
					LLVMValueRef element = builder.BuildLoad2(value.Type.LLVMType, elementPtr, name);
					closure.Identifiers.Add(name, new TypedValueValue(value.Type, element));
				}
				
				VisitFunctionBlock(closure, closure.Statements);
			}
			
			TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
			TypedTypeClosurePointer closureType = new(closureStructType, functionType);
			
			builder.PositionAtEnd(originalBlock);
			LLVMValueRef closurePtr = builder.BuildAlloca(closureType.Type.LLVMType, "closure");
			
			// Pack the closure for the function
			{
				LLVMValueRef functionPtr = builder.BuildStructGEP2(closureType.Type.LLVMType, closurePtr, 0, "function");
				builder.BuildStore(function, functionPtr);
				
				LLVMValueRef closureEnvPtr = builder.BuildStructGEP2(closureType.Type.LLVMType, closurePtr, 1, "closure_env_ptr");
				LLVMValueRef closureEnv = builder.BuildAlloca(closureEnvType.LLVMType, "closure_env");
				int paramIndex = 0;
				foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
				{
					LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnv, (uint)paramIndex++, name);
					builder.BuildStore(value.Value, elementPtr);
				}
				LLVMValueRef closureEnvCasted = builder.BuildBitCast(closureEnv, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), "closure_env_casted");
				builder.BuildStore(closureEnvCasted, closureEnvPtr);
			}
			
			return new TypedValueValue(closureType, closurePtr);
		}
	}
}