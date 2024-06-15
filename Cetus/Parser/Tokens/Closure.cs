using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class Closure() : TokenSplit(new LiteralToken("{"), new LiteralToken(";"), new LiteralToken("}"), new ParameterExpressionToken("statements")), TypedValue, IHasIdentifiers
{
	public List<FunctionCallContext> Statements;
	public ProgramContext Program { get; set; }
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<TypedType> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
	public LLVMBasicBlockRef Block;
	public TypedValue? ReturnValue;
	
	public TypedType Type { get; }
	public LLVMValueRef LLVMValue { get; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		if (typeHint is TypedTypeCompilerClosure)
		{
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
			builder.PositionAtEnd(block);
			
			VisitFunctionBlock(this, Statements);
			
			builder.PositionAtEnd(originalBlock);
		}
		else if (typeHint is TypedTypeClosurePointer pointer)
		{
			Dictionary<string, TypedValue> uniqueClosureIdentifiers = ((NestedDictionary<string, TypedValue>)context.Identifiers).ThisDict;
			TypedTypeStruct closureEnvType = new(LLVMTypeRef.CreateStruct(uniqueClosureIdentifiers.Values.Select(type => type.Type.LLVMType).ToArray(), false));
			
			TypedTypeFunction functionType = pointer.BlockType ?? new DefinedFunctionCall("closure_block", ((TypedTypeCompilerClosure)typeHint).ReturnType, [(new TypedTypePointer(new TypedTypeChar()), "data")], null);
			LLVMValueRef function = module.AddFunction("closure_block", functionType.LLVMType);
			function.Linkage = LLVMLinkage.LLVMInternalLinkage;
			
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			builder.PositionAtEnd(function.AppendBasicBlock("entry"));
			
			// Unpack the closure environment in the function
			{
				LLVMValueRef closureEnvPtr = function.GetParam(0);
				int paramIndex = 0;
				foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
				{
					LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnvPtr, (uint)paramIndex++, name);
					LLVMValueRef element = builder.BuildLoad2(value.Type.LLVMType, elementPtr, name);
					Identifiers.Add(name, new TypedValueValue(value.Type, element));
				}
				
				VisitFunctionBlock(this, Statements);
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
					builder.BuildStore(value.LLVMValue, elementPtr);
				}
				LLVMValueRef closureEnvCasted = builder.BuildBitCast(closureEnv, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), "closure_env_casted");
				builder.BuildStore(closureEnvCasted, closureEnvPtr);
			}
		}
		else
			throw new Exception("Expected closure");
	}
}