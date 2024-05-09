using System.Diagnostics;
using JetBrains.Annotations;
using LLVMSharp.Interop;
using Identifiers = System.Collections.Generic.Dictionary<string, Cetus.TypedValue>;

namespace Cetus;

public class Visitor
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	
	public static readonly TypedType VoidType = new TypedTypeVoid();
	public static readonly TypedType IntType = new TypedTypeInt();
	public static readonly TypedType BoolType = new TypedTypeBool();
	public static readonly TypedType CharType = new TypedTypeChar();
	public static readonly TypedType FloatType = new TypedTypeFloat();
	public static readonly TypedType DoubleType = new TypedTypeDouble();
	public static readonly TypedType StringType = new TypedTypePointer(CharType);
	public static readonly TypedType CompilerStringType = new TypedTypeCompilerString();
	
	public static readonly TypedValue Void = new TypedValueType(VoidType);
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0, false));
	
	private Identifiers globalIdentifiers = new()
	{
		{ "Void", new TypedValueType(VoidType) },
		{ "Float", new TypedValueType(FloatType) },
		{ "Double", new TypedValueType(DoubleType) },
		{ "Char", new TypedValueType(CharType) },
		{ "Int", new TypedValueType(IntType) },
		{ "String", new TypedValueType(StringType) },
		{ "CompilerString", new TypedValueType(CompilerStringType) },
		{ "Bool", new TypedValueType(BoolType) },
		{ "True", TrueValue },
		{ "False", FalseValue },
		
		{ "Declare", new TypedValueType(new TypedTypeFunctionDeclare()) },
		{ "Assign", new TypedValueType(new TypedTypeFunctionAssign()) },
		{ "Return", new TypedValueType(new TypedTypeFunctionReturn()) },
		{ "ReturnVoid", new TypedValueType(new TypedTypeFunctionReturnVoid()) },
		{ "Add", new TypedValueType(new TypedTypeFunctionAdd()) },
		{ "LessThan", new TypedValueType(new TypedTypeFunctionLessThan()) },
		{ "While", new TypedValueType(new TypedTypeFunctionWhile()) },
	};
	
	private List<string> referencedLibs = [];
	
	public Visitor()
	{
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVMModuleRef.CreateWithName("mainModule");
		builder = LLVMBuilderRef.Create(module.Context);
	}
	
	public void Generate(Parser.ProgramContext program)
	{
		VisitProgram(program, globalIdentifiers);
		Dump();
		module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	private void VisitProgram(Parser.ProgramContext context, Identifiers identifiers)
	{
		foreach (Parser.IProgramStatementContext statement in context.ProgramStatements)
			VisitProgramStatement(statement, identifiers);
	}
	
	private void VisitProgramStatement(Parser.IProgramStatementContext context, Identifiers identifiers)
	{
		if (context is Parser.FunctionDefinitionContext functionDefinition)
			VisitFunctionDefinition(functionDefinition, identifiers);
		else if (context is Parser.ExternFunctionDeclarationContext externFunctionDeclaration)
			VisitExternFunctionDeclaration(externFunctionDeclaration, identifiers);
		else if (context is Parser.ExternStructDeclarationContext externStructDeclaration)
			VisitExternStructDeclaration(externStructDeclaration, identifiers);
		else if (context is Parser.IncludeLibraryContext includeLibrary)
			VisitIncludeLibrary(includeLibrary);
		else if (context is Parser.DelegateDeclarationContext delegateDeclaration)
			VisitDelegateDeclaration(delegateDeclaration, identifiers);
		else if (context is Parser.ConstVariableDefinitionContext constVariableDefinition)
			VisitConstVariableDefinition(constVariableDefinition, identifiers);
		else
			throw new Exception("Unknown program statement type: " + context.GetType());
	}
	
	private TypedValue VisitValue(Parser.IValueContext context, Identifiers identifiers, TypedType? typeHint)
	{
		if (context is Parser.IntegerContext integer)
			return VisitInteger(integer);
		if (context is Parser.FloatContext @float)
			return VisitFloat(@float);
		if (context is Parser.DoubleContext @double)
			return VisitDouble(@double);
		if (context is Parser.StringContext @string)
			return VisitString(@string, typeHint);
		if (context is Parser.ClosureContext closure)
			return VisitClosure(closure, identifiers, typeHint);
		if (context is Parser.NullContext)
			return VisitNull(typeHint);
		if (context is Parser.ValueIdentifierContext valueIdentifier)
			return VisitValueIdentifier(valueIdentifier, identifiers, typeHint);
		if (context is LiteralContext closureEnvironment)
			return closureEnvironment.Value;
		throw new Exception("Unknown value type: " + context.GetType());
	}
	
	private TypedValue VisitInteger(Parser.IntegerContext context)
	{
		return new TypedValueValue(IntType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)context.Value, true));
	}
	
	private TypedValue VisitFloat(Parser.FloatContext context)
	{
		return new TypedValueValue(FloatType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, context.Value));
	}
	
	private TypedValue VisitDouble(Parser.DoubleContext context)
	{
		return new TypedValueValue(DoubleType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, context.Value));
	}
	
	private TypedValue VisitString(Parser.StringContext context, TypedType? typeHint)
	{
		return typeHint is TypedTypeCompilerString
			? new TypedValueCompilerString(context.Value)
			: new TypedValueValue(StringType, builder.BuildGlobalStringPtr(context.Value, context.Value.Length == 0 ? "emptyString" : context.Value));
	}
	
	private TypedValue VisitClosure(Parser.ClosureContext context, Identifiers identifiers, TypedType? typeHint)
	{
		if (typeHint is TypedTypeCompilerClosure compilerClosureType)
		{
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
			TypedValueCompilerClosure closure = new(compilerClosureType, block);
			builder.PositionAtEnd(block);
			
			Identifiers blockIdentifiers = new(identifiers);
			blockIdentifiers["Return"] = new TypedValueType(new TypedTypeFunctionReturnCompilerClosure(closure));
			blockIdentifiers["ReturnVoid"] = new TypedValueType(new TypedTypeFunctionReturnVoidCompilerClosure(closure));
			VisitFunctionBlock(context.Statements, blockIdentifiers);
			
			builder.PositionAtEnd(originalBlock);
			return closure;
		}
		else
		{
			Identifiers uniqueClosureIdentifiers = identifiers.Except(globalIdentifiers).ToDictionary();
			TypedTypeStruct closureEnvType = new(LLVMTypeRef.CreateStruct(uniqueClosureIdentifiers.Values.Select(type => type.Type.LLVMType).ToArray(), false));
			
			TypedTypeFunction functionType = (typeHint as TypedTypeClosurePointer)?.BlockType ?? new TypedTypeFunction("closure_block", ((TypedTypeCompilerClosure)typeHint).ReturnType, [new TypedTypePointer(new TypedTypeChar())], null);
			LLVMValueRef function = module.AddFunction("closure_block", functionType.LLVMType);
			function.Linkage = LLVMLinkage.LLVMInternalLinkage;
			
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			builder.PositionAtEnd(function.AppendBasicBlock("entry"));
			
			// Unpack the closure environment in the function
			{
				Identifiers closureIdentifiers = new(globalIdentifiers);
				LLVMValueRef closureEnvPtr = function.GetParam(0);
				int paramIndex = 0;
				foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
				{
					LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnvPtr, (uint)paramIndex++, name);
					LLVMValueRef element = builder.BuildLoad2(value.Type.LLVMType, elementPtr, name);
					closureIdentifiers.Add(name, new TypedValueValue(value.Type, element));
				}
				
				VisitFunctionBlock(context.Statements, closureIdentifiers);
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
			
			TypedValue result = new TypedValueValue(closureType, closurePtr);
			return result;
		}
	}
	
	private TypedValue VisitNull(TypedType? typeHint)
	{
		if (typeHint == null)
			throw new Exception("Cannot infer type of null");
		return new TypedValueValue(typeHint, LLVMValueRef.CreateConstNull(typeHint.LLVMType));
	}
	
	private TypedValue VisitValueIdentifier(Parser.ValueIdentifierContext context, Identifiers identifiers, TypedType? typeHint)
	{
		string name = context.ValueName;
		if (!identifiers.TryGetValue(name, out TypedValue? result))
			throw new Exception($"Identifier '{name}' not found");
		
		if (typeHint is not null and not TypedTypePointer && result.Type is TypedTypePointer resultTypePointer)
		{
			LLVMValueRef value = builder.BuildLoad2(resultTypePointer.PointerType.LLVMType, result.Value, name);
			result = new TypedValueValue(resultTypePointer.PointerType, value);
		}
		
		if (typeHint is not null && !TypedTypeExtensions.TypesEqual(result.Type, typeHint))
			throw new Exception($"Type mismatch in value of '{name}', expected {typeHint.LLVMType} but got {result.Type.LLVMType}");
		
		return result;
	}
	
	private void VisitDelegateDeclaration(Parser.DelegateDeclarationContext context, Identifiers identifiers)
	{
		string name = context.FunctionName;
		TypedType returnType = VisitTypeIdentifier(context.ReturnType, identifiers);
		TypedType[] paramTypes = context.Parameters.Select(param => param.ParameterType)
			.Select(paramType => VisitTypeIdentifier(paramType, identifiers))
			.ToArray();
		bool isVarArg = context.VarArg is not null;
		TypedType varArgType = isVarArg ? VisitTypeIdentifier(context.VarArg.ParameterType, identifiers) : null;
		TypedTypeFunction functionType = new(name, returnType, paramTypes, varArgType);
		TypedValue result = new TypedValueType(functionType);
		identifiers.Add(name, result);
	}
	
	private TypedType VisitTypeIdentifier(Parser.TypeIdentifierContext context, Identifiers identifiers)
	{
		string name = context.TypeName;
		if (name == "Closure")
		{
			TypedTypeFunction functionType = new("block", context.InnerType is not null ? VisitTypeIdentifier(context.InnerType, identifiers) : VoidType, [new TypedTypePointer(new TypedTypeChar())], null);
			TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
			return new TypedTypeClosurePointer(closureStructType, functionType);
		}
		else if (name == "CompilerClosure")
		{
			return new TypedTypeCompilerClosure(context.InnerType is not null ? VisitTypeIdentifier(context.InnerType, identifiers) : VoidType);
		}
		else
		{
			if (!identifiers.TryGetValue(name, out TypedValue? result))
				throw new Exception($"Type '{name}' not found");
			TypedType type = result.Type;
			for (int i = 0; i < context.PointerCount; ++i)
				type = new TypedTypePointer(type);
			return type;
		}
	}
	
	private void VisitFunctionDefinition(Parser.FunctionDefinitionContext context, Identifiers identifiers)
	{
		string name = context.FunctionName;
		TypedType returnType = VisitTypeIdentifier(context.ReturnType, identifiers);
		TypedType[] paramTypes = context.Parameters.Select(param => param.ParameterType)
			.Select(paramType => VisitTypeIdentifier(paramType, identifiers))
			.ToArray();
		bool isVarArg = context.VarArg is not null;
		TypedType varArgType = isVarArg ? VisitTypeIdentifier(context.VarArg.ParameterType, identifiers) : null;
		TypedTypeFunction functionType = new(name, returnType, paramTypes, varArgType);
		LLVMValueRef function = module.AddFunction(name, functionType.LLVMType);
		function.Linkage = LLVMLinkage.LLVMExternalLinkage;
		
		Identifiers newIdentifiers = new(identifiers);
		
		for (int i = 0; i < context.Parameters.Count; ++i)
		{
			string parameterName = context.Parameters[i].ParameterName;
			TypedType parameterType = VisitTypeIdentifier(context.Parameters[i].ParameterType, identifiers);
			LLVMValueRef param = function.GetParam((uint)i);
			param.Name = parameterName;
			newIdentifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		builder.PositionAtEnd(function.AppendBasicBlock("entry"));
		
		VisitFunctionBlock(context.Statements, newIdentifiers);
		
		function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		TypedValue result = new TypedValueValue(functionType, function);
		identifiers.Add(name, result);
	}
	
	private void VisitFunctionBlock(IEnumerable<Parser.FunctionStatementContext> contexts, Identifiers identifiers)
	{
		foreach (Parser.FunctionStatementContext? statement in contexts)
			VisitFunctionCall(statement.Call, identifiers);
	}
	
	private void VisitIncludeLibrary(Parser.IncludeLibraryContext context)
	{
		referencedLibs.Add(context.LibraryName);
	}
	
	private void VisitExternFunctionDeclaration(Parser.ExternFunctionDeclarationContext context, Identifiers identifiers)
	{
		string name = context.FunctionName;
		TypedType returnType = VisitTypeIdentifier(context.ReturnType, identifiers);
		TypedType[] paramTypes = context.Parameters.Select(param => param.ParameterType)
			.Select(paramType => VisitTypeIdentifier(paramType, identifiers))
			.ToArray();
		bool isVarArg = context.VarArg is not null;
		TypedType varArgType = isVarArg ? VisitTypeIdentifier(context.VarArg.ParameterType, identifiers) : null;
		TypedTypeFunction functionType = new(name, returnType, paramTypes, varArgType);
		LLVMValueRef function = module.AddFunction(name, functionType.LLVMType);
		TypedValue result = new TypedValueValue(functionType, function);
		identifiers.Add(name, result);
	}
	
	private void VisitExternStructDeclaration(Parser.ExternStructDeclarationContext context, Identifiers identifiers)
	{
		string name = context.StructName;
		LLVMTypeRef @struct = LLVMContextRef.Global.CreateNamedStruct(name);
		TypedValue result = new TypedValueType(new TypedTypeStruct(@struct));
		identifiers.Add(name, result);
	}
	
	private void VisitConstVariableDefinition(Parser.ConstVariableDefinitionContext context, Identifiers identifiers)
	{
		string name = context.VariableName;
		TypedType type = VisitTypeIdentifier(context.Type, identifiers);
		TypedValue value = VisitValue(context.Value, identifiers, type);
		LLVMValueRef global = module.AddGlobal(type.LLVMType, name);
		global.Linkage = LLVMLinkage.LLVMInternalLinkage;
		global.Initializer = value.Value;
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), global);
		identifiers.Add(name, result);
	}
	
	private TypedValue VisitExpression(Parser.IExpressionContext context, Identifiers identifiers, TypedType? typeHint)
	{
		if (context is Parser.FunctionCallContext functionCall)
			return VisitFunctionCall(functionCall, identifiers);
		if (context is Parser.IValueContext value)
			return VisitValue(value, identifiers, typeHint);
		throw new Exception("Unknown expression type: " + context.GetType());
	}
	
	public class LiteralContext(TypedValue value) : Parser.IValueContext
	{
		public TypedValue Value => value;
	}
	
	private TypedValue VisitFunctionCall(Parser.FunctionCallContext context, Identifiers identifiers)
	{
		TypedValue function = VisitExpression(context.Function, identifiers, null);
		List<Parser.IExpressionContext> arguments = context.Arguments;
		TypedTypeFunction functionType;
		if (function.Type is TypedTypeClosurePointer closurePtr)
		{
			functionType = closurePtr.BlockType;
			
			LLVMValueRef functionPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.Value, 0, "functionPtrPtr");
			LLVMValueRef functionPtr = builder.BuildLoad2(functionType.Pointer().LLVMType, functionPtrPtr, "functionPtr");
			LLVMValueRef environmentPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.Value, 1, "environmentPtrPtr");
			LLVMValueRef environmentPtr = builder.BuildLoad2(CharType.Pointer().LLVMType, environmentPtrPtr, "environmentPtr");
			
			function = new TypedValueValue(functionType, functionPtr);
			arguments = arguments.Prepend(new LiteralContext(new TypedValueValue(CharType.Pointer(), environmentPtr))).ToList();
		}
		else if (function.Type is TypedTypePointer functionPointer)
		{
			functionType = (TypedTypeFunction)functionPointer.PointerType;
			function = new TypedValueValue(functionType, builder.BuildLoad2(functionType.LLVMType, function.Value, "functionValue"));
		}
		else
			functionType = (TypedTypeFunction)function.Type;
		string functionName = functionType.FunctionName;
		
		if (functionType.IsVarArg ? arguments.Count < functionType.NumParams : arguments.Count != functionType.NumParams)
			throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(functionType.IsVarArg ? "at least " : "")}{functionType.NumParams} but got {arguments.Count}");
		
		IEnumerable<TypedType> paramTypeHints = functionType.ParamTypes;
		if (arguments.Count > functionType.NumParams)
			paramTypeHints = paramTypeHints.Concat(Enumerable.Range(0, arguments.Count - functionType.NumParams).Select(_ => functionType.VarArgType!));
		TypedValue[] args = arguments
			.Zip(paramTypeHints, (arg, param) => VisitExpression(arg, identifiers, param))
			.ToArray();
		
		foreach ((TypedValue arg, TypedType type) in args.Zip(functionType.ParamTypes))
			if (!TypedTypeExtensions.TypesEqual(arg.Type, type))
				throw new Exception($"Argument type mismatch in call to '{functionName}', expected {type} but got {arg.Type.LLVMType}");
		
		return functionType.Call(builder, function, identifiers, args);
	}
	
	[UsedImplicitly]
	private void Printf(string message, Identifiers identifiers, params TypedValue[] args)
	{
		VisitFunctionCall(new Parser.FunctionCallContext
		{
			Function = new Parser.ValueIdentifierContext { ValueName = "printf" },
			Arguments = args.Select(arg => new LiteralContext(arg) as Parser.IExpressionContext).Prepend(new Parser.StringContext { Value = message }).ToList()
		}, identifiers);
	}
	
	public void Optimize()
	{
		LLVMPassManagerRef passManager = LLVMPassManagerRef.Create();
		passManager.AddConstantMergePass();
		passManager.AddInstructionCombiningPass();
		passManager.AddPromoteMemoryToRegisterPass();
		passManager.AddGVNPass();
		passManager.AddCFGSimplificationPass();
		passManager.Run(module);
		passManager.Dispose();
	}
	
	public void Dispose()
	{
		module.Dispose();
		builder.Dispose();
	}
	
	public void Dump()
	{
		module.Dump();
	}
	
	public void Compile(string filename = "main")
	{
		const string targetTriple = "x86_64-pc-windows-msvc";
		LLVMTargetRef target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
		LLVMTargetMachineRef targetMachine = target.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		targetMachine.EmitToFile(module, filename + ".s", LLVMCodeGenFileType.LLVMAssemblyFile);
		
		CompileSFileToExe(filename + ".s", filename + ".exe");
	}
	
	private void CompileSFileToExe(string sFilePath, string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "clang",
			Arguments = $"{sFilePath} -o {exeFilePath} -L lib {string.Join(" ", referencedLibs.Select(lib => "-l" + lib))}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the compilation process");
		
		process.WaitForExit();
		
		string output = process.StandardOutput.ReadToEnd();
		Console.Write(output);
		string error = process.StandardError.ReadToEnd();
		Console.Write(error);
		
		if (process.ExitCode != 0)
			throw new Exception($"Compilation failed with exit code {process.ExitCode}");
	}
}

public interface TypedValue
{
	public TypedType Type { get; }
	public LLVMValueRef Value { get; }
}

public class TypedValueValue(TypedType type, LLVMValueRef value) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => value;
	public override string ToString() => value.ToString();
}

public class TypedValueType(TypedType type) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a type");
	public override string ToString() => type.ToString()!;
}

public class TypedValueCompilerString(string value) : TypedValue
{
	public TypedType Type => new TypedTypeCompilerString();
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a compiler string");
	public string StringValue => value;
	public override string ToString() => value;
}

public class TypedValueCompilerClosure(TypedTypeCompilerClosure type, LLVMBasicBlockRef block) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a compiler closure");
	public LLVMBasicBlockRef Block => block;
	public TypedValue? ReturnValue;
}

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
}

public static class TypedTypeExtensions
{
	public static bool TypesEqual(TypedValue lhs, TypedValue rhs) => TypesEqual(lhs.Type.LLVMType, rhs.Type.LLVMType);
	public static bool TypesEqual(TypedType lhs, TypedType rhs) => TypesEqual(lhs.LLVMType, rhs.LLVMType);
	public static bool TypesEqual(LLVMTypeRef lhs, LLVMTypeRef rhs)
	{
		while (lhs.Kind == LLVMTypeKind.LLVMPointerTypeKind && rhs.Kind == LLVMTypeKind.LLVMPointerTypeKind)
		{
			lhs = lhs.ElementType;
			rhs = rhs.ElementType;
		}
		
		if (lhs.Kind != rhs.Kind)
			return false;
		
		if (lhs.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
			return lhs.IntWidth == rhs.IntWidth;
		
		if (lhs.Kind == LLVMTypeKind.LLVMFunctionTypeKind)
			return TypesEqual(lhs.ReturnType, rhs.ReturnType) && lhs.ParamTypesCount == rhs.ParamTypesCount && lhs.ParamTypes.Zip(rhs.ParamTypes).All(pair => TypesEqual(pair.First, pair.Second));
		
		return true;
	}
	
	public static TypedTypePointer Pointer(this TypedType type) => new(type);
}

public class TypedTypePointer(TypedType pointerType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(pointerType.LLVMType, 0);
	public TypedType PointerType => pointerType;
	public override string ToString() => pointerType + "*";
}

public class TypedTypeInt : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int32;
	public override string ToString() => "Int";
}

public class TypedTypeBool : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int1;
	public override string ToString() => "Bool";
}

public class TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int8;
	public override string ToString() => "Char";
}

public class TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Void;
	public override string ToString() => "Void";
}

public class TypedTypeFloat : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Float;
	public override string ToString() => "Float";
}

public class TypedTypeDouble : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Double;
	public override string ToString() => "Double";
}

public class TypedTypeFunction(string name, TypedType returnType, IReadOnlyCollection<TypedType> paramTypes, TypedType? varArgType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, paramTypes.Select(paramType => paramType.LLVMType).ToArray(), IsVarArg);
	public string FunctionName => name;
	public TypedType ReturnType => returnType;
	public IEnumerable<TypedType> ParamTypes => paramTypes;
	public int NumParams => paramTypes.Count;
	public TypedType? VarArgType => varArgType;
	public bool IsVarArg => varArgType is not null;
	public override string ToString() => LLVMType.ToString();
	
	public virtual TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		return new TypedValueValue(ReturnType, builder.BuildCall2(LLVMType, function.Value, args.Select(arg => arg.Value).ToArray(), ReturnType is TypedTypeVoid ? "" : name + "Call"));
	}
}

public class TypedTypeFunctionDeclare() : TypedTypeFunction("Declare", Visitor.VoidType, [Visitor.CompilerStringType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		TypedType type = Visitor.IntType;
		string name = ((TypedValueCompilerString)args[0]).StringValue;
		TypedValue value = args[1];
		if (!TypedTypeExtensions.TypesEqual(type, value.Type))
			throw new Exception($"Type mismatch in assignment to '{name}', expected {type.LLVMType} but got {value.Type.LLVMType}");
		LLVMValueRef variable = builder.BuildAlloca(type.LLVMType, name);
		builder.BuildStore(value.Value, variable);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		identifiers.Add(name, result);
		return Visitor.Void;
	}
}

public class TypedTypeFunctionAssign() : TypedTypeFunction("Assign", Visitor.VoidType, [Visitor.IntType.Pointer(), Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		builder.BuildStore(args[1].Value, args[0].Value);
		return Visitor.Void;
	}
}

public class TypedTypeFunctionReturn() : TypedTypeFunction("Return", Visitor.VoidType, [Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		builder.BuildRet(args[0].Value);
		return Visitor.Void;
	}
}

public class TypedTypeFunctionReturnVoid() : TypedTypeFunction("ReturnVoid", Visitor.VoidType, [], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		builder.BuildRetVoid();
		return Visitor.Void;
	}
}

public class TypedTypeFunctionReturnCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("Return", Visitor.VoidType, [Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		closure.ReturnValue = args[0];
		return Visitor.Void;
	}
}

public class TypedTypeFunctionReturnVoidCompilerClosure(TypedValueCompilerClosure closure) : TypedTypeFunction("ReturnVoid", Visitor.VoidType, [], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		closure.ReturnValue = null;
		return Visitor.Void;
	}
}

public class TypedTypeFunctionAdd() : TypedTypeFunction("Add", Visitor.IntType, [Visitor.IntType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		LLVMValueRef sum = builder.BuildAdd(args[0].Value, args[1].Value, "addtmp");
		return new TypedValueValue(Visitor.IntType, sum);
	}
}

public class TypedTypeFunctionLessThan() : TypedTypeFunction("LessThan", Visitor.IntType, [Visitor.IntType, Visitor.IntType], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		LLVMValueRef lessThan = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args[0].Value, args[1].Value, "lttmp");
		LLVMValueRef lessThanExt = builder.BuildZExt(lessThan, LLVMTypeRef.Int32, "lttmpint");
		return new TypedValueValue(Visitor.IntType, lessThanExt);
	}
}

public class TypedTypeFunctionWhile() : TypedTypeFunction("While", Visitor.VoidType, [new TypedTypeCompilerClosure(Visitor.IntType), new TypedTypeCompilerClosure(Visitor.VoidType)], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, Identifiers identifiers, params TypedValue[] args)
	{
		TypedValueCompilerClosure conditionClosure = (TypedValueCompilerClosure)args[0];
		TypedValueCompilerClosure loopClosure = (TypedValueCompilerClosure)args[1];
		LLVMBasicBlockRef merge = builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		builder.BuildBr(conditionClosure.Block);
		
		builder.PositionAtEnd(conditionClosure.Block);
		TypedValue conditionValue = conditionClosure.ReturnValue ?? throw new Exception("While condition is missing a return value");
		LLVMValueRef condition = builder.BuildTrunc(conditionValue.Value, LLVMTypeRef.Int1, "whileCondition");
		builder.BuildCondBr(condition, loopClosure.Block, merge);
		
		builder.PositionAtEnd(loopClosure.Block);
		builder.BuildBr(conditionClosure.Block);
		
		builder.PositionAtEnd(merge);
		return Visitor.Void;
	}
}

public class TypedTypeClosurePointer(TypedTypeStruct type, TypedTypeFunction blockType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(type.LLVMType, 0);
	public TypedTypeStruct Type => type;
	public TypedTypeFunction BlockType => blockType;
	public override string ToString() => LLVMType.ToString();
}

public class TypedTypeCompilerClosure(TypedType returnType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreateFunction(returnType.LLVMType, [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false), 0);
	public TypedType ReturnType => returnType;
	public override string ToString() => LLVMType.ToString();
}

public class TypedTypeCompilerString : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
	public override string ToString() => "CompilerString";
}

public class TypedTypeStruct(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	public override string ToString() => LLVMType.ToString();
}