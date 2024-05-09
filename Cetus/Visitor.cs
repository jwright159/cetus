using System.Diagnostics;
using System.Runtime.InteropServices;
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
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0, false));
	
	private Identifiers globalIdentifiers = new()
	{
		{ "void", new TypedValueType(VoidType) },
		{ "float", new TypedValueType(FloatType) },
		{ "double", new TypedValueType(DoubleType) },
		{ "char", new TypedValueType(CharType) },
		{ "int", new TypedValueType(IntType) },
		{ "string", new TypedValueType(StringType) },
		{ "bool", new TypedValueType(BoolType) },
		{ "true", TrueValue },
		{ "false", FalseValue },
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
		module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string output);
		Console.WriteLine(output);
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
			return VisitString(@string);
		if (context is Parser.FunctionBlockContext functionBlock)
			return VisitClosure(functionBlock, identifiers);
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
	
	private TypedValue VisitString(Parser.StringContext context)
	{
		return new TypedValueValue(StringType, builder.BuildGlobalStringPtr(context.Value, context.Value.Length == 0 ? "emptyString" : context.Value));
	}
	
	private TypedValue VisitClosure(Parser.FunctionBlockContext context, Identifiers identifiers)
	{
		Identifiers uniqueClosureIdentifiers = identifiers.Except(globalIdentifiers).ToDictionary();
		TypedTypeStruct closureEnvType = new(LLVMContextRef.Global.CreateNamedStruct("closure_env"));
		closureEnvType.LLVMType.StructSetBody(uniqueClosureIdentifiers.Values.Select(type => LLVMTypeRef.CreatePointer(type.Type.LLVMType, 0)).ToArray(), false);
		
		TypedTypeFunction functionType = new("closure_block", VoidType, [new TypedTypePointer(new TypedTypeChar())], null);
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
			
			VisitFunctionBlock(context.Statements, closureIdentifiers, new Dictionary<string, LLVMBasicBlockRef>());
			builder.BuildRetVoid();
			
			function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		}
		
		TypedTypeClosurePointer closureType = AddOrGetClosureType(functionType);
		
		builder.PositionAtEnd(originalBlock);
		LLVMValueRef closurePtr = builder.BuildAlloca(closureType.Type.LLVMType, "closure");
		
		// Pack the closure for the function
		{
			LLVMValueRef functionPtr = builder.BuildStructGEP2(closureType.Type.LLVMType, closurePtr, 0, "function");
			builder.BuildStore(function, functionPtr);
			
			LLVMValueRef closureEnvPtr = builder.BuildStructGEP2(closureType.Type.LLVMType, closurePtr, 1, "closure_env");
			LLVMValueRef closureEnv = builder.BuildAlloca(closureEnvType.LLVMType, "closure_env");
			int paramIndex = 0;
			foreach ((string name, TypedValue value) in uniqueClosureIdentifiers)
			{
				LLVMValueRef elementPtr = builder.BuildStructGEP2(closureEnvType.LLVMType, closureEnv, (uint)paramIndex++, name);
				builder.BuildStore(value.Value, elementPtr);
			}
			builder.BuildStore(closureEnv, closureEnvPtr);
		}
		
		TypedValue result = new TypedValueValue(closureType, closurePtr);
		return result;
	}
	
	private TypedTypeClosurePointer AddOrGetClosureType(TypedTypeFunction functionType)
	{
		if (globalIdentifiers.Values.FirstOrDefault(type => type.Type is TypedTypeClosurePointer closureType && TypedTypeExtensions.TypesEqual(closureType.BlockType, functionType))?.Type is TypedTypeClosurePointer existingClosureType)
			return existingClosureType;
		else
		{
			TypedTypeStruct closureStructType = new(LLVMContextRef.Global.CreateNamedStruct($"closure_{functionType.ReturnType}"));
			closureStructType.LLVMType.StructSetBody([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false);
			TypedTypeClosurePointer closureType = new(closureStructType, functionType);
			globalIdentifiers.Add($"closure_{functionType.ReturnType}", new TypedValueType(closureType));
			return closureType;
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
			return AddOrGetClosureType(new TypedTypeFunction("block", context.InnerType is not null ? VisitTypeIdentifier(context.InnerType, identifiers) : VoidType, [new TypedTypePointer(new TypedTypeChar())], null));
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
		
		VisitFunctionBlock(context.Statements, newIdentifiers, new Dictionary<string, LLVMBasicBlockRef>());
		builder.BuildRetVoid();
		
		function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		TypedValue result = new TypedValueValue(functionType, function);
		identifiers.Add(name, result);
	}
	
	private void VisitFunctionBlock(IEnumerable<Parser.IFunctionStatementContext> contexts, Identifiers identifiers, Dictionary<string, LLVMBasicBlockRef> blocks)
	{
		foreach (Parser.IFunctionStatementContext? statement in contexts)
			VisitFunctionStatement(statement, identifiers, blocks);
	}
	
	private void VisitFunctionStatement(Parser.IFunctionStatementContext context, Identifiers identifiers, Dictionary<string, LLVMBasicBlockRef> blocks)
	{
		if (context is Parser.FunctionCallContext functionCall)
			VisitFunctionCall(functionCall, identifiers);
		else if (context is Parser.CompilerStatementContext compilerStatement)
			VisitCompilerStatement(compilerStatement, identifiers, blocks);
		else
			throw new Exception("Unknown function statement type: " + context.GetType());
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
			
			LLVMValueRef closure = builder.BuildLoad2(closurePtr.Type.LLVMType, function.Value, "actual_closure");
			LLVMValueRef functionPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, closure, 0, "functionPtrPtr");
			LLVMValueRef functionPtr = builder.BuildLoad2(LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), functionPtrPtr, "functionPtr");
			//LLVMValueRef functionValue = builder.BuildLoad2(functionType.LLVMType, functionPtr, "functionValue");
			LLVMValueRef environmentPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, closure, 1, "environment");
			
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
		
		LLVMValueRef result = builder.BuildCall2(functionType.LLVMType, function.Value, args.Select(arg => arg.Value).ToArray(), functionType.ReturnType is TypedTypeVoid ? "" : functionName + "Call");
		return new TypedValueValue(functionType.ReturnType, result);
	}
	
	private void VisitCompilerStatement(Parser.CompilerStatementContext context, Identifiers identifiers, Dictionary<string, LLVMBasicBlockRef> blocks)
	{
		if (context.Tokens[0] == "decl")
		{
			TypedType type = IntType;
			string name = context.Tokens[1];
			TypedValue value = VisitInteger(new Parser.IntegerContext { Value = int.Parse(context.Tokens[2]) });
			if (!TypedTypeExtensions.TypesEqual(type, value.Type))
				throw new Exception($"Type mismatch in assignment to '{name}', expected {type.LLVMType} but got {value.Type.LLVMType}");
			LLVMValueRef variable = builder.BuildAlloca(type.LLVMType, name);
			builder.BuildStore(value.Value, variable);
			TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
			identifiers.Add(name, result);
		}
		else if (context.Tokens[0] == "asgn")
		{
			TypedValue variable = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[1] }, identifiers, IntType.Pointer());
			TypedValue value = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[2] }, identifiers, IntType);
			builder.BuildStore(value.Value, variable.Value);
		}
		else if (context.Tokens[0] == "rtn")
		{
			builder.BuildRet(VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[1] }, identifiers, null).Value);
		}
		else if (context.Tokens[0] == "br")
		{
			builder.BuildBr(blocks[context.Tokens[1]]);
		}
		else if (context.Tokens[0] == "decllabel")
		{
			blocks.Add(context.Tokens[1], builder.InsertBlock.Parent.AppendBasicBlock(context.Tokens[1]));
		}
		else if (context.Tokens[0] == "uselabel")
		{
			builder.PositionAtEnd(blocks[context.Tokens[1]]);
		}
		else if (context.Tokens[0] == "brif")
		{
			builder.BuildCondBr(VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[1] }, identifiers, IntType).Value, blocks[context.Tokens[2]], blocks[context.Tokens[3]]);
		}
		else if (context.Tokens[0] == "add")
		{
			TypedValue lhs = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[1] }, identifiers, IntType);
			TypedValue rhs = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[2] }, identifiers, IntType);
			LLVMValueRef variable = builder.BuildAlloca(IntType.LLVMType, context.Tokens[3]);
			builder.BuildStore(builder.BuildAdd(lhs.Value, rhs.Value, "addtmp"), variable);
			TypedValue result = new TypedValueValue(IntType.Pointer(), variable);
			identifiers.Add(context.Tokens[3], result);
		}
		else if (context.Tokens[0] == "lt")
		{
			TypedValue lhs = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[1] }, identifiers, IntType);
			TypedValue rhs = VisitValueIdentifier(new Parser.ValueIdentifierContext { ValueName = context.Tokens[2] }, identifiers, IntType);
			LLVMValueRef variable = builder.BuildAlloca(IntType.LLVMType, context.Tokens[3]);
			builder.BuildStore(builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, lhs.Value, rhs.Value, "lttmp"), variable);
			TypedValue result = new TypedValueValue(IntType.Pointer(), variable);
			identifiers.Add(context.Tokens[3], result);
		}
		else
			throw new Exception("Unknown compiler statement type: " + context.Tokens[0]);
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

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
}

public static class TypedTypeExtensions
{
	public static void CheckForTypes<T>(this T type, TypedValue lhs, TypedValue rhs) where T : TypedType
	{
		if (lhs.Type is not T) throw new Exception($"Lhs is a {lhs.Type}, not a {type}");
		if (rhs.Type is not T) throw new Exception($"Rhs is a {rhs.Type}, not a {type}");
	}
	
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
		
		return true;
	}
	
	public static TypedTypePointer Pointer(this TypedType type) => new(type);
}

public class TypedTypePointer(TypedType pointerType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(pointerType.LLVMType, 0);
	public TypedType PointerType => pointerType;
	
	public override string ToString() => pointerType + "*";
	
	public void CheckForTypes(TypedValue lhs, TypedValue rhs)
	{
		if (lhs.Type is not TypedTypePointer lhsPointer) throw new Exception($"Lhs is a {lhs.Type}, not a {typeof(TypedTypePointer)}");
		if (!TypedTypeExtensions.TypesEqual(lhsPointer.PointerType, PointerType)) throw new Exception($"Lhs is a {lhsPointer.PointerType} pointer, not a {pointerType} pointer");
		if (rhs.Type is not TypedTypePointer rhsPointer) throw new Exception($"Rhs is a {rhs.Type}, not a {typeof(TypedTypePointer)}");
		if (!TypedTypeExtensions.TypesEqual(rhsPointer.PointerType, PointerType)) throw new Exception($"Rhs is a {rhsPointer.PointerType} pointer, not a {pointerType} pointer");
	}
}

public class TypedTypeInt : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int32;
	public override string ToString() => "int";
}

public class TypedTypeBool : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int1;
	public override string ToString() => "bool";
}

public class TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int8;
	public override string ToString() => "char";
}

public class TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Void;
	public override string ToString() => "void";
}

public class TypedTypeFloat : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Float;
	public override string ToString() => "float";
}

public class TypedTypeDouble : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Double;
	public override string ToString() => "double";
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
}

public class TypedTypeClosurePointer(TypedTypeStruct type, TypedTypeFunction blockType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(type.LLVMType, 0);
	public TypedTypeStruct Type => type;
	public TypedTypeFunction BlockType => blockType;
	public override string ToString() => LLVMType.ToString();
}

public class TypedTypeStruct(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	public override string ToString() => LLVMType.ToString();
}