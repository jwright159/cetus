using CTes;

string code = File.ReadAllText("sample.ctes");
IEnumerable<Token> tokens = Lexer.Lex(code);
IEnumerable<Expression> expressions = Parser.Parse(tokens);
CodeGenerator.GenerateAndCompile(expressions);