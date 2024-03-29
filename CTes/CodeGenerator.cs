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
	private LLVMValueRef mainFunction;
	
	private LLVMTypeRef pointerType = LLVM.PointerType(LLVM.Int8Type(), 0);
	
	private Dictionary<string, TypedValue> identifiers = new()
	{
		{ "void", LLVM.VoidType() },
		{ "double", LLVM.DoubleType() },
		{ "char", LLVM.Int8Type() },
		{ "int", LLVM.Int32Type() },
	};
	
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
		LLVMTypeRef[] paramTypes = [];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.VoidType(), paramTypes, false);
		mainFunction = LLVM.AddFunction(module, "main", functionType);
		
		LLVMBasicBlockRef entry = mainFunction.AppendBasicBlock("entry");
		LLVM.PositionBuilderAtEnd(builder, entry);
		
		identifiers.Add("NULL", new TypedValue(false, pointerType, LLVM.ConstPointerNull(pointerType)));
		
		Visit(program);
		
		LLVM.BuildRetVoid(builder);
		
		LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	public override TypedValue VisitDecimalNumber(CTesParser.DecimalNumberContext context)
	{
		return LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text), true);
	}
	
	public override TypedValue VisitHexNumber(CTesParser.HexNumberContext context)
	{
		return LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text[2..], NumberStyles.HexNumber), true);
	}
	
	public override TypedValue VisitString(CTesParser.StringContext context)
	{
		string str = context.CHARACTER()?.GetText() ?? "";
		str = System.Text.RegularExpressions.Regex.Unescape(str);
		string name = string.Concat(str.Where(char.IsLetter));
		return LLVM.BuildGlobalStringPtr(builder, str, (name.Length == 0 ? "some" : name) + "String");
	}
	
	public override TypedValue VisitAdd(CTesParser.AddContext context)
	{
		return LLVM.BuildAdd(builder, Visit(context.lhs).Value, Visit(context.rhs).Value, "addtmp");
	}
	
	public override TypedValue VisitValueIdentifier(CTesParser.ValueIdentifierContext context)
	{
		string name = context.GetText();
		if (!identifiers.TryGetValue(name, out TypedValue result))
			throw new Exception($"Identifier '{name}' not found");
		return result;
	}
	
	public override TypedValue VisitTypeIdentifier(CTesParser.TypeIdentifierContext context)
	{
		string name = context.name.Text;
		if (!identifiers.TryGetValue(name, out TypedValue result))
			throw new Exception($"Type '{name}' not found");
		return context._pointers.Aggregate(result.Type, (type, _) => LLVM.PointerType(type, 0));
	}
	
	public override TypedValue VisitFunctionCall(CTesParser.FunctionCallContext context)
	{
		TypedValue function = Visit(context.function);
		string functionName = context.function.GetText();
		
		if (function.Value.IsAFunction().Pointer == IntPtr.Zero)
			throw new Exception($"Value '{functionName}' is not a function");
		
		TypedValue[] args = context.arguments().expression().Select(Visit).ToArray();
		
		bool isVarArg = function.Type.IsFunctionVarArg;
		if (isVarArg ? args.Length < function.Value.CountParams() : args.Length != function.Value.CountParams())
			throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(isVarArg ? "at least " : "")}{function.Value.CountParams()} but got {args.Length}");
		
		foreach ((TypedValue arg, LLVMTypeRef type) in args.Zip(function.Type.GetParamTypes()))
			if (arg.Type.TypeKind != type.TypeKind)
				throw new Exception($"Argument type mismatch in call to '{functionName}', expected {type.TypeKind} but got {arg.Type.TypeKind}");
		
		return LLVM.BuildCall(builder, function.Value, args.Select(arg => arg.Value).ToArray(), function.Type.GetReturnType().TypeKind == LLVMTypeKind.LLVMVoidTypeKind ? "" : functionName + "Call");
	}
	
	public override TypedValue VisitFunctionDefinition(CTesParser.FunctionDefinitionContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef returnType = Visit(context.returnType).Type;
		CTesParser.ParameterContext[] parameters = context.parameters()._params.Where(param => param.name != null).ToArray();
		LLVMTypeRef[] paramTypes = parameters.Select(param => param.type).Select(Visit).Select(type => type.Type).ToArray();
		bool isVarArg = context.parameters()._params.Any(param => param.varArg != null);
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		LLVMValueRef function = LLVM.AddFunction(module, name, functionType);
		LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
		
		for (int i = 0; i < context.parameters()._params.Count; ++i)
		{
			string argumentName = context.parameters()._params[i].name.Text;
			LLVMValueRef param = function.GetParam((uint)i);
			LLVM.SetValueName(param, argumentName);
			identifiers.Add(argumentName, param);
		}
		
		LLVM.PositionBuilderAtEnd(builder, function.AppendBasicBlock("entry"));
		
		try
		{
			foreach (CTesParser.StatementContext? statement in context.statement())
				Visit(statement);
			
			if (context.returnStatement() != null)
				LLVM.BuildRet(builder, Visit(context.returnStatement().expression()).Value);
			else
				LLVM.BuildRetVoid(builder);
		}
		catch (Exception)
		{
			LLVM.DeleteFunction(function);
			throw;
		}
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		identifiers.Add(name, new TypedValue(false, functionType, function));
		
		LLVM.PositionBuilderAtEnd(builder, mainFunction.GetLastBasicBlock());
		
		return function;
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
		identifiers.Add(name, new TypedValue(false, functionType, function));
		return function;
	}
	
	public override TypedValue VisitExternVariableDeclaration(CTesParser.ExternVariableDeclarationContext context)
	{
		string name = context.name.Text;
		LLVMTypeRef type = Visit(context.type).Type;
		LLVMValueRef global = LLVM.AddGlobal(module, type, name);
		global.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
		TypedValue value = global;
		identifiers.Add(name, value);
		return value;
	}
	
	public override TypedValue VisitDereference(CTesParser.DereferenceContext context)
	{
		TypedValue pointer = Visit(context.expression());
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
		identifiers.Add(name, result);
		return result;
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

public readonly struct TypedValue(bool isType, LLVMTypeRef type, LLVMValueRef value)
{
	public bool IsType { get; } = isType;
	public LLVMTypeRef Type { get; } = type;
	public LLVMValueRef Value { get; } = value;
	
	public override string ToString() => IsType ? Type.ToString() : Value.ToString();
	
	public static implicit operator TypedValue(LLVMValueRef value) => new(false, value.IsAFunction().Pointer == IntPtr.Zero ? value.TypeOf() : value.TypeOf().GetElementType(), value);
	public static implicit operator TypedValue(LLVMTypeRef type) => new(true, type, default);
}