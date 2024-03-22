using CTes;

string code = "2 + 3";

IEnumerable<Token> tokens = Lexer.Lex(code);
IEnumerable<Expression> expressions = Parser.Parse(tokens);
CodeGenerator.GenerateAndCompile(expressions);