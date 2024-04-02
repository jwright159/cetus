using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using CTes.Antlr;
using LLVMSharp;

namespace CTes;

public class CodeGenerator : CTesBaseVisitor<TypedValue>
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	
	private Dictionary<string, TypedValue> autoDerefGlobalIdentifiers = new();
	private Dictionary<string, TypedValue> noDerefGlobalIdentifiers = new()
	{
		{ "void", (TypedValueType)LLVM.VoidType() },
		{ "float", (TypedValueType)LLVM.FloatType() },
		{ "double", (TypedValueType)LLVM.DoubleType() },
		{ "char", (TypedValueType)LLVM.Int8Type() },
		{ "int", (TypedValueType)LLVM.Int32Type() },
		{ "long", (TypedValueType)LLVM.Int64Type() },
		{ "string", (TypedValueType)LLVM.PointerType(LLVM.Int8Type(), 0) },
		{ "bool", (TypedValueType)LLVM.Int1Type() },
		{ "true", (TypedValueValue)LLVM.ConstInt(LLVM.Int1Type(), 1, false) },
		{ "false", (TypedValueValue)LLVM.ConstInt(LLVM.Int1Type(), 0, false) },
	};
	private Dictionary<string, TypedValue> autoDerefLocalIdentifiers = new();
	private Dictionary<string, TypedValue> noDerefLocalIdentifiers = new();
	
	private List<string> referencedLibs = [];
	
	public CodeGenerator()
	{
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVM.ModuleCreateWithName("mainModule");
		builder = LLVM.CreateBuilder();
	}
	
	public void Generate(CTesParser.ProgramContext program)
	{
		Visit(program);
		LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	public override TypedValue VisitDecimalNumber(CTesParser.DecimalNumberContext context)
	{
		return (TypedValueValue)LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text), true);
	}
	
	public override TypedValue VisitHexNumber(CTesParser.HexNumberContext context)
	{
		return (TypedValueValue)LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text[2..], NumberStyles.HexNumber), true);
	}
	
	public override TypedValue VisitFloatNumber(CTesParser.FloatNumberContext context)
	{
		return (TypedValueValue)LLVM.ConstReal(LLVM.FloatType(), float.Parse(context.digits.Text[..^1]));
	}
	
	public override TypedValue VisitDoubleNumber(CTesParser.DoubleNumberContext context)
	{
		return (TypedValueValue)LLVM.ConstReal(LLVM.DoubleType(), double.Parse(context.digits.Text));
	}
	
	public override TypedValue VisitString(CTesParser.StringContext context)
	{
		string str = context.STRING().GetText()[1..^1];
		str = System.Text.RegularExpressions.Regex.Unescape(str);
		string name = string.Concat(str.Where(char.IsLetter));
		return (TypedValueValue)LLVM.BuildGlobalStringPtr(builder, str, (name.Length == 0 ? "some" : name) + "String");
	}
	
	public override TypedValue VisitNull(CTesParser.NullContext context)
	{
		return new TypedValueNull();
	}
	
	public override TypedValue VisitDelegateDeclaration(CTesParser.DelegateDeclarationContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef returnType = Visit(context.returnType).Type(default);
		CTesParser.ParameterContext[] parameters = context.parameters()._params.Where(param => param.name != null).ToArray();
		LLVMTypeRef[] paramTypes = parameters.Select(param => param.type).Select(Visit).Select(type => type.Type(default)).ToArray();
		bool isVarArg = context.parameters()._params.Any(param => param.varArg != null);
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		TypedValue result = new TypedValueType(functionType);
		noDerefGlobalIdentifiers.Add(name, result);
		return result;
	}
	
	public override TypedValue VisitAddition(CTesParser.AdditionContext context)
	{
		return (TypedValueValue)LLVM.BuildAdd(builder, Visit(context.lhs).Value(LLVM.Int32Type()), Visit(context.rhs).Value(LLVM.Int32Type()), "addtmp");
	}
	
	public override TypedValue VisitValueIdentifier(CTesParser.ValueIdentifierContext context)
	{
		string name = context.GetText();
		
		if (noDerefLocalIdentifiers.TryGetValue(name, out TypedValue? result))
			return result;
		
		if (autoDerefLocalIdentifiers.TryGetValue(name, out result))
			return (TypedValueValue)LLVM.BuildLoad(builder, result.Value(null), "loadvartmp");
		
		if (noDerefGlobalIdentifiers.TryGetValue(name, out result))
			return result;
		
		if (autoDerefGlobalIdentifiers.TryGetValue(name, out result))
			return (TypedValueValue)LLVM.BuildLoad(builder, result.Value(null), "loadvartmp");
		
		throw new Exception($"Identifier '{name}' not found");
	}
	
	public override TypedValue VisitTypeIdentifier(CTesParser.TypeIdentifierContext context)
	{
		string name = context.name.Text;
		if (!noDerefGlobalIdentifiers.TryGetValue(name, out TypedValue? result))
			throw new Exception($"Type '{name}' not found");
		return (TypedValueType)context._pointers.Aggregate(result.Type(null), (type, _) => LLVM.PointerType(type, 0));
	}
	
	public override TypedValue VisitFunctionCall(CTesParser.FunctionCallContext context)
	{
		TypedValue function = Visit(context.function);
		string functionName = context.function.GetText();
		LLVMTypeRef functionType = function.Type(null).TypeKind == LLVMTypeKind.LLVMPointerTypeKind ? function.Type(null).GetElementType() : function.Type(null);
		
		if (functionType.TypeKind != LLVMTypeKind.LLVMFunctionTypeKind)
			throw new Exception($"Value '{functionName}' is a {functionType.TypeKind}, not a function");
		
		TypedValue[] args = context.arguments().expression().Select(Visit).ToArray();
		
		bool isVarArg = functionType.IsFunctionVarArg;
		if (isVarArg ? args.Length < functionType.CountParamTypes() : args.Length != functionType.CountParamTypes())
			throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(isVarArg ? "at least " : "")}{functionType.CountParamTypes()} but got {args.Length}");
		
		foreach ((TypedValue arg, LLVMTypeRef type) in args.Zip(functionType.GetParamTypes()))
			if (arg.Type(null).TypeKind != type.TypeKind)
				throw new Exception($"Argument type mismatch in call to '{functionName}', expected {type.TypeKind} but got {arg.Type(null).TypeKind}");
		
		LLVM.BuildCall(builder, function.Value(null), args.Select(arg => arg.Value(null)).ToArray(), functionType.GetReturnType().TypeKind == LLVMTypeKind.LLVMVoidTypeKind ? "" : functionName + "Call");
		return default;
	}
	
	public override TypedValue VisitIfStatement(CTesParser.IfStatementContext context)
	{
		LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
		LLVMBasicBlockRef thenBlock = LLVM.AppendBasicBlock(functionBlock, "ifThen");
		LLVMBasicBlockRef elseBlock = LLVM.AppendBasicBlock(functionBlock, "ifElse");
		LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "ifMerge");
		
		LLVMValueRef condition = Visit(context.condition).Value(null);
		LLVM.BuildCondBr(builder, condition, thenBlock, elseBlock);
		
		LLVM.PositionBuilderAtEnd(builder, thenBlock);
		Visit(context.thenStatements);
		LLVM.BuildBr(builder, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, elseBlock);
		if (context.elseStatements != null)
			Visit(context.elseStatements);
		LLVM.BuildBr(builder, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, mergeBlock);
		return default;
	}
	
	public override TypedValue VisitWhileStatement(CTesParser.WhileStatementContext context)
	{
		LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
		LLVMBasicBlockRef conditionBlock = LLVM.AppendBasicBlock(functionBlock, "whileCondition");
		LLVMBasicBlockRef bodyBlock = LLVM.AppendBasicBlock(functionBlock, "whileBody");
		LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "whileMerge");
		
		LLVM.BuildBr(builder, conditionBlock);
		
		LLVM.PositionBuilderAtEnd(builder, conditionBlock);
		LLVMValueRef condition = Visit(context.condition).Value(null);
		LLVM.BuildCondBr(builder, condition, bodyBlock, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, bodyBlock);
		Visit(context.thenStatements);
		LLVM.BuildBr(builder, conditionBlock);
		
		LLVM.PositionBuilderAtEnd(builder, mergeBlock);
		return default;
	}
	
	public override TypedValue VisitFunctionDefinition(CTesParser.FunctionDefinitionContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef returnType = Visit(context.returnType).Type(null);
		CTesParser.ParameterContext[] parameters = context.parameters()._params.Where(param => param.name != null).ToArray();
		LLVMTypeRef[] paramTypes = parameters.Select(param => param.type).Select(Visit).Select(type => type.Type(null)).ToArray();
		bool isVarArg = context.parameters()._params.Any(param => param.varArg != null);
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		LLVMValueRef function = LLVM.AddFunction(module, name, functionType);
		LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
		
		noDerefLocalIdentifiers.Clear();
		autoDerefLocalIdentifiers.Clear();
		
		for (int i = 0; i < context.parameters()._params.Count; ++i)
		{
			string argumentName = context.parameters()._params[i].name.Text;
			LLVMValueRef param = function.GetParam((uint)i);
			LLVM.SetValueName(param, argumentName);
			noDerefLocalIdentifiers.Add(argumentName, (TypedValueValue)param);
		}
		
		LLVM.PositionBuilderAtEnd(builder, function.AppendBasicBlock("entry"));
		
		try
		{
			foreach (CTesParser.StatementContext? statement in context.statement())
				Visit(statement);
			LLVM.BuildRetVoid(builder);
		}
		catch (Exception)
		{
			LLVM.DeleteFunction(function);
			throw;
		}
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		TypedValue result = new TypedValueValue(functionType, function);
		noDerefGlobalIdentifiers.Add(name, result);
		
		return result;
	}
	
	public override TypedValue VisitReturnStatement(CTesParser.ReturnStatementContext context)
	{
		return context.expression() != null ? LLVM.BuildRet(builder, Visit(context.expression()).Value(null)) : LLVM.BuildRetVoid(builder);
	}
	
	public override TypedValue VisitIncludeLibrary(CTesParser.IncludeLibraryContext context)
	{
		referencedLibs.Add(context.lib.Text);
		return base.VisitIncludeLibrary(context);
	}
	
	public override TypedValue VisitExternFunctionDeclaration(CTesParser.ExternFunctionDeclarationContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef returnType = Visit(context.returnType).Type;
		CTesParser.ParameterContext[] parameters = context.parameters()._params.Where(param => param.name != null).ToArray();
		LLVMTypeRef[] paramTypes = parameters.Select(param => param.type).Select(Visit).Select(type => type.Type).ToArray();
		bool isVarArg = context.parameters()._params.Any(param => param.varArg != null);
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		LLVMValueRef function = LLVM.AddFunction(module, name, functionType);
		noDerefGlobalIdentifiers.Add(name, new TypedValue(false, functionType, function));
		return function;
	}
	
	public override TypedValue VisitExternStructDeclaration(CTesParser.ExternStructDeclarationContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef @struct = LLVM.StructCreateNamed(LLVM.GetGlobalContext(), name);
		noDerefGlobalIdentifiers.Add(name, new TypedValue(true, @struct, default));
		return @struct;
	}
	
	public override TypedValue VisitExternVariableDeclaration(CTesParser.ExternVariableDeclarationContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef type = Visit(context.type).Type;
		LLVMValueRef global = LLVM.AddGlobal(module, type, name);
		global.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
		TypedValue value = global;
		autoDerefGlobalIdentifiers.Add(name, value);
		return value;
	}
	
	public override TypedValue VisitDereference(CTesParser.DereferenceContext context)
	{
		TypedValue pointer = Visit(context.operators3());
		if (pointer.Type.TypeKind != LLVMTypeKind.LLVMPointerTypeKind)
			throw new Exception("Cannot dereference a non-pointer type");
		return LLVM.BuildLoad(builder, pointer.Value, "loadtmp");
	}
	
	public override TypedValue VisitConstVariableDeclaration(CTesParser.ConstVariableDeclarationContext context)
	{
		string name = context.name.Text;
		TypedValue value = Visit(context.value());
		LLVMValueRef global = LLVM.AddGlobal(module, value.Type, name);
		global.SetLinkage(LLVMLinkage.LLVMInternalLinkage);
		global.SetInitializer(value.Value);
		TypedValue result = global;
		autoDerefGlobalIdentifiers.Add(name, result);
		return result;
	}
	
	public override TypedValue VisitAssignmentStatement(CTesParser.AssignmentStatementContext context)
	{
		TypedValue type = Visit(context.type);
		string name = context.name.Text;
		TypedValue value = Visit(context.val);
		if (type.Type.TypeKind != value.Type.TypeKind)
			throw new Exception($"Type mismatch in assignment to '{name}', expected {type.Type.TypeKind} but got {value.Type.TypeKind}");
		LLVMValueRef variable = LLVM.BuildAlloca(builder, type.Type, name);
		LLVM.BuildStore(builder, value.Value, variable);
		TypedValue result = new(false, type.Type, variable);
		autoDerefLocalIdentifiers.Add(name, result);
		return result;
	}
	
	public override TypedValue VisitNegation(CTesParser.NegationContext context)
	{
		return LLVM.BuildNot(builder, Visit(context.operators3()).Value, "negtmp");
	}
	
	public override TypedValue VisitEquivalence(CTesParser.EquivalenceContext context)
	{
		return LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, Visit(context.lhs).Value, Visit(context.rhs).Value, "eqtmp");
	}
	
	public override TypedValue VisitInequivalence(CTesParser.InequivalenceContext context)
	{
		return LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, Visit(context.lhs).Value, Visit(context.rhs).Value, "neqtmp");
	}
	
	public void Optimize()
	{
		LLVMPassManagerRef passManager = LLVM.CreatePassManager();
		LLVM.AddConstantPropagationPass(passManager);
		LLVM.AddInstructionCombiningPass(passManager);
		LLVM.AddPromoteMemoryToRegisterPass(passManager);
		LLVM.AddGVNPass(passManager);
		LLVM.AddCFGSimplificationPass(passManager);
		LLVM.RunPassManager(passManager, module);
		LLVM.DisposePassManager(passManager);
	}
	
	public void Dispose()
	{
		LLVM.DisposeModule(module);
		LLVM.DisposeBuilder(builder);
	}
	
	public void Dump()
	{
		LLVM.DumpModule(module);
	}
	
	public void CompileAndRun(string filename = "main")
	{
		const string targetTriple = "x86_64-pc-windows-msvc";
		if (LLVM.GetTargetFromTriple(targetTriple, out LLVMTargetRef target, out string error))
			throw new Exception(error);
		LLVMTargetMachineRef targetMachine = LLVM.CreateTargetMachine(target, targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		LLVM.SetModuleDataLayout(module, LLVM.CreateTargetDataLayout(targetMachine));
		LLVM.SetTarget(module, targetTriple);
		
		IntPtr asmFilename = Marshal.StringToHGlobalAnsi(filename + ".s");
		if (LLVM.TargetMachineEmitToFile(targetMachine, module, asmFilename, LLVMCodeGenFileType.LLVMAssemblyFile, out error))
			throw new Exception(error);
		Marshal.FreeHGlobal(asmFilename);
		
		CompileSFileToExe(filename + ".s", filename + ".exe");
		RunExe(filename + ".exe");
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
	
	private static void RunExe(string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = exeFilePath,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		startInfo.EnvironmentVariables["PATH"] += ";lib";
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the execution process");
		
		process.WaitForExit();
		
		string output = process.StandardOutput.ReadToEnd();
		Console.Write(output);
		string error = process.StandardError.ReadToEnd();
		Console.Write(error);
	}
}

public interface TypedValue
{
	public LLVMTypeRef Type(LLVMTypeRef inference);
	public LLVMValueRef Value(LLVMTypeRef inference);
}

public readonly struct TypedValueValue(LLVMTypeRef type, LLVMValueRef value) : TypedValue
{
	public LLVMTypeRef Type(LLVMTypeRef inference) => type;
	public LLVMValueRef Value(LLVMTypeRef inference) => value;
	public override string ToString() => value.ToString();
	public static implicit operator TypedValueValue(LLVMValueRef value) => new(value.IsAFunction().Pointer == IntPtr.Zero ? value.TypeOf() : value.TypeOf().GetElementType(), value);
}

public readonly struct TypedValueType(LLVMTypeRef type) : TypedValue
{
	public LLVMTypeRef Type(LLVMTypeRef inference) => type;
	public LLVMValueRef Value(LLVMTypeRef inference) => throw new Exception("Cannot get the value of a type");
	public override string ToString() => type.ToString();
	public static implicit operator TypedValueType(LLVMTypeRef type) => new(type);
}

public readonly struct TypedValueNull : TypedValue
{
	public LLVMTypeRef Type(LLVMTypeRef inference) => inference.Pointer != IntPtr.Zero ? inference : throw new Exception("Cannot infer the type of null");
	public LLVMValueRef Value(LLVMTypeRef inference) => inference.Pointer != IntPtr.Zero ? LLVM.ConstPointerNull(inference) : throw new Exception("Cannot infer the type of null");
	public override string ToString() => "null";
}