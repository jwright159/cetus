﻿using Cetus;

Lexer lexer = new(File.ReadAllText("sample.cetus"));
Parser parser = new(lexer);

Parser.ProgramContext program = parser.Parse();
Visitor visitor = new();
visitor.Generate(program);
visitor.Dump();
Console.WriteLine();

// visitor.Optimize();
// visitor.Dump();
// Console.WriteLine();

visitor.CompileAndRun();

visitor.Dispose();