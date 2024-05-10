using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseClosure(FunctionContext context, TypedType? typeHint, out TypedValue? closure)
	{
		int startIndex = lexer.Index;
		if (!lexer.Eat<LeftBrace>() || typeHint is not TypedTypeClosurePointer and not TypedTypeCompilerClosure)
		{
			lexer.Index = startIndex;
			closure = null;
			return new Result.TokenRuleFailed("Expected closure", lexer.Line, lexer.Column);
		}
		lexer.Index = startIndex;
		
		if (typeHint is TypedTypeCompilerClosure compilerClosureType)
		{
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
			TypedValueCompilerClosure compilerClosure = new(compilerClosureType, block);
			closure = compilerClosure;
			builder.PositionAtEnd(block);
			
			FunctionContext closureContext = new(context);
			closureContext.Identifiers["Return"] = new TypedValueType(new TypedTypeFunctionReturnCompilerClosure(compilerClosure));
			closureContext.Identifiers["ReturnVoid"] = new TypedValueType(new TypedTypeFunctionReturnVoidCompilerClosure(compilerClosure));
			
			Result result = ParseFunctionBlock(closureContext);
			
			builder.PositionAtEnd(originalBlock);
			
			return Result.WrapPassable("Invalid closure", result);
		}
		else
		{
			Dictionary<string, TypedValue> uniqueClosureIdentifiers = context.Identifiers.Except(context.Program.Identifiers).ToDictionary();
			TypedTypeStruct closureEnvType = new(LLVMTypeRef.CreateStruct(uniqueClosureIdentifiers.Values.Select(type => type.Type.LLVMType).ToArray(), false));
			
			TypedTypeFunction functionType = (typeHint as TypedTypeClosurePointer)?.BlockType ?? new TypedTypeFunction("closure_block", ((TypedTypeCompilerClosure)typeHint).ReturnType, [new TypedTypePointer(new TypedTypeChar())], null, null);
			LLVMValueRef function = module.AddFunction("closure_block", functionType.LLVMType);
			function.Linkage = LLVMLinkage.LLVMInternalLinkage;
			
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			builder.PositionAtEnd(function.AppendBasicBlock("entry"));
			
			// Unpack the closure environment in the function
			Result result;
			{
				FunctionContext closureContext = new(context.Program);
				LLVMValueRef closureEnvPtr = function.GetParam(0);
				int paramIndex = 0;
				foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
				{
					LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnvPtr, (uint)paramIndex++, name);
					LLVMValueRef element = builder.BuildLoad2(value.Type.LLVMType, elementPtr, name);
					closureContext.Identifiers.Add(name, new TypedValueValue(value.Type, element));
				}
				
				result = ParseFunctionBlock(closureContext);
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
			
			closure = new TypedValueValue(closureType, closurePtr);
			
			return Result.WrapPassable("Invalid closure", result);
		}
	}
}