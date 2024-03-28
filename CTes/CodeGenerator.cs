using System.Diagnostics;
using System.Runtime.InteropServices;
using CTes.Antlr;
using LLVMSharp;

namespace CTes;

public class CodeGenerator : CTesBaseVisitor<LLVMValueRef>
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	private LLVMValueRef mainFunction;
	
	private Dictionary<string, LLVMTypeRef> typeIdentifiers = new()
	{
		{ "double", LLVM.DoubleType() }
	};
	
	private Dictionary<string, LLVMValueRef> valueIdentifiers = new();
	
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
		
		LLVMBasicBlockRef entry = LLVM.AppendBasicBlock(mainFunction, "entry");
		LLVM.PositionBuilderAtEnd(builder, entry);
		
		GeneratePrintfBindings();
		GenerateOpenGLBindings();
		
		Visit(program);
		
		LLVM.BuildRetVoid(builder);
	}
	
	private void GeneratePrintfBindings()
	{
		LLVMTypeRef[] paramTypes = [LLVM.PointerType(LLVM.Int8Type(), 0)];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.Int32Type(), paramTypes, true);
		LLVMValueRef function = LLVM.AddFunction(module, "printf", functionType);
		valueIdentifiers.Add("Print", function);
	}
	
	private void GenerateOpenGLBindings()
	{
		LLVMTypeRef[] paramTypes = [];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.VoidType(), paramTypes, false);
		LLVMValueRef function = LLVM.AddFunction(module, "glfwInit", functionType);
		valueIdentifiers.Add("glfwInit", function);
	}
	
	public override LLVMValueRef VisitNumber(CTesParser.NumberContext context)
	{
		return LLVM.ConstReal(LLVM.DoubleType(), double.Parse(context.GetText()));
	}
	
	public override LLVMValueRef VisitString(CTesParser.StringContext context)
	{
		string str = context.CHARACTER().GetText();
		str = System.Text.RegularExpressions.Regex.Unescape(str);
		return LLVM.BuildGlobalStringPtr(builder, str, string.Concat(str.Where(char.IsLetter)) + "String");
	}
	
	public override LLVMValueRef VisitAdd(CTesParser.AddContext context)
	{
		return LLVM.BuildFAdd(builder, Visit(context.lhs), Visit(context.rhs), "addtmp");
	}
	
	public override LLVMValueRef VisitValueIdentifier(CTesParser.ValueIdentifierContext context)
	{
		string name = context.GetText();
		if (!valueIdentifiers.TryGetValue(name, out LLVMValueRef result))
			throw new Exception($"Identifier '{name}' not found");
		return result;
	}
	
	public override LLVMValueRef VisitFunctionCall(CTesParser.FunctionCallContext context)
	{
		LLVMValueRef function = Visit(context.function);
		LLVMTypeRef functionType = function.TypeOf().GetElementType();
		string functionName = context.function.GetText();
		
		if (function.IsAFunction().IsNull())
			throw new Exception($"Value '{functionName}' is not a function");
		
		LLVMValueRef[] args = context.arguments().expression().Select(Visit).ToArray();
		
		bool isVarArg = LLVM.IsFunctionVarArg(functionType);
		if (isVarArg ? args.Length < function.CountParams() : args.Length != function.CountParams())
			throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(isVarArg ? "at least " : "")}{function.CountParams()} but got {args.Length}");
		
		foreach ((LLVMValueRef arg, LLVMValueRef typ) in args.Zip(function.GetParams()))
			if (arg.TypeOf().TypeKind != typ.TypeOf().TypeKind)
				throw new Exception($"Argument type mismatch in call to '{functionName}', expected {typ.TypeOf().TypeKind} but got {arg.TypeOf().TypeKind}");
		
		return LLVM.BuildCall(builder, function, args, functionName + "Call");
	}
	
	public override LLVMValueRef VisitFunctionDefinition(CTesParser.FunctionDefinitionContext context)
	{
		string name = context.functionName.Text;
		LLVMValueRef function = LLVM.AddFunction(module, name, LLVM.FunctionType(LLVM.DoubleType(), context.parameters()._types.Select(typ => typeIdentifiers[typ.GetText()]).ToArray(), false));
		LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
		
		for (int i = 0; i < context.parameters()._args.Count; ++i)
		{
			string argumentName = context.parameters()._args[i].Text;
			
			LLVMValueRef param = LLVM.GetParam(function, (uint)i);
			LLVM.SetValueName(param, argumentName);
			
			valueIdentifiers.Add(argumentName, param);
		}
		
		LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlock(function, "entry"));
		
		try
		{
			foreach (CTesParser.StatementContext? statement in context.statement())
				Visit(statement);
			
			if (context.returnStatement() != null)
				LLVM.BuildRet(builder, Visit(context.returnStatement().expression()));
			else
				LLVM.BuildRetVoid(builder);
		}
		catch (Exception)
		{
			LLVM.DeleteFunction(function);
			throw;
		}
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		valueIdentifiers.Add(name, function);
		
		LLVM.PositionBuilderAtEnd(builder, mainFunction.GetLastBasicBlock());
		
		return function;
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
	
	private static readonly string[] Libs = ["glfw3dll.lib"];
	private static void CompileSFileToExe(string sFilePath, string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "clang",
			Arguments = $"{sFilePath} -o {exeFilePath} {string.Join(" ", Libs.Select(lib => $"lib/{lib}"))}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the compilation process");
		
		process.WaitForExit();
		
		if (process.ExitCode != 0)
		{
			string error = process.StandardError.ReadToEnd();
			throw new Exception($"Compilation failed with exit code {process.ExitCode}: {error}");
		}
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