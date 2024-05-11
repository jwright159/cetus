using System.Diagnostics;
using Cetus.Parser.Contexts;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using JetBrains.Annotations;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	private Lexer lexer;
	
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
	
	private List<string> referencedLibs = [];
	
	public Parser(Lexer lexer)
	{
		this.lexer = lexer;
		
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVMModuleRef.CreateWithName("mainModule");
		builder = LLVMBuilderRef.Create(module.Context);
	}
	
	public void Generate()
	{
		ProgramContext context = new()
		{
			Identifiers =
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
			}
		};
		Result result = ParseProgram(context);
		Dump();
		if (result is not Result.Ok)
			Console.WriteLine(result);
		module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	[UsedImplicitly]
	private void Printf(string message, FunctionContext context, params TypedValue[] args)
	{
		TypedValue function = context.Identifiers["printf"];
		TypedTypeFunction functionType = (TypedTypeFunction)function.Type;
		TypedValueValue messageValue = new(StringType, builder.BuildGlobalStringPtr(message, "message"));
		args = args.Prepend(messageValue).ToArray();
		functionType.Call(builder, function, context, args);
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