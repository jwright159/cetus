grammar Cetus;

/*
 * Parser Rules
 */

program: programChunk* ;
programChunk
    : includeLibrary
    | externFunctionDeclaration
    | externStructDeclaration
    | externVariableDeclaration
    | constVariableDeclaration
    | delegateDeclaration
    | functionDefinition
    ;

includeLibrary: 'include' lib=WORD SEMICOLON ;
externFunctionDeclaration: 'extern' returnType=typeIdentifier name=WORD parameters SEMICOLON ;
externStructDeclaration: 'extern' 'struct' name=WORD SEMICOLON ;
externVariableDeclaration: 'extern' type=typeIdentifier name=WORD SEMICOLON ;
constVariableDeclaration: 'const' type=typeIdentifier name=WORD EQUALS value SEMICOLON ;
delegateDeclaration: 'delegate' returnType=typeIdentifier name=WORD parameters SEMICOLON ;
functionDefinition: returnType=typeIdentifier name=WORD parameters OPEN_BRACE statement* CLOSE_BRACE ;

statement
    : ifStatement
    | whileStatement
    | assignmentStatement
    | functionCallStatement
    | returnStatement
    ;
ifStatement: 'if' OPEN_PAREN condition=expression CLOSE_PAREN OPEN_BRACE thenStatements=statement* CLOSE_BRACE ('else' OPEN_BRACE elseStatements=statement* CLOSE_BRACE)? ;
whileStatement: 'while' OPEN_PAREN condition=expression CLOSE_PAREN OPEN_BRACE thenStatements=statement* CLOSE_BRACE ;
assignmentStatement: type=typeIdentifier name=WORD EQUALS val=expression SEMICOLON ;
functionCallStatement: functionCall SEMICOLON ;
returnStatement: 'return' expression? SEMICOLON ;


expression: operators1 ;

operators1: equivalence | inequivalence | operators2 ;
equivalence: lhs=operators2 EQUALITY rhs=operators2 ;
inequivalence: lhs=operators2 INEQUALITY rhs=operators2 ;

operators2: addition | operators3 ;
addition: lhs=operators3 PLUS rhs=operators3 ;

operators3: dereference | negation | operators4 ;
dereference: ASTERISK operators3 ;
negation: NOT operators3 ;

operators4: functionCall | operators5 ;
functionCall: function=operators5 arguments ;

operators5: subexpression | value ;
subexpression: OPEN_PAREN operators1 CLOSE_PAREN ;


arguments: OPEN_PAREN (args+=expression (COMMA args+=expression)* COMMA?)? CLOSE_PAREN ;
parameters: OPEN_PAREN (params+=parameter (COMMA params+=parameter)* COMMA?)? CLOSE_PAREN ;
parameter
    : type=typeIdentifier name=WORD
    | varArg='...'
    ;

value: string | number | valueIdentifier ;
valueIdentifier: WORD ;
number
    : decimalNumber
    | hexNumber
    | floatNumber
    | doubleNumber
    ;
decimalNumber: digits=NUMBER ;
hexNumber: digits=HEX_NUMBER ;
floatNumber: digits=FLOAT_NUMBER ;
doubleNumber: digits=DOUBLE_NUMBER ;
typeIdentifier: name=WORD (pointers+=ASTERISK)* ;
string: STRING ;

/*
 * Lexer Rules
 */

ASTERISK: '*' ;
PLUS: '+' ;
EQUALS: '=' ;
EQUALITY: '==' ;
INEQUALITY: '!=' ;
COMMA: ',' ;
NOT: '!' ;
OPEN_PAREN: '(' ;
CLOSE_PAREN: ')' ;
OPEN_BRACE: '{' ;
CLOSE_BRACE: '}' ;
SEMICOLON: ';' ;
WORD: [a-zA-Z_][a-zA-Z0-9_]* ;
NUMBER: [0-9]+ ;
HEX_NUMBER: '0x' [0-9a-fA-F]+ ;
FLOAT_NUMBER: [0-9]+ '.' [0-9]+ 'f' ;
DOUBLE_NUMBER: [0-9]+ '.' [0-9]+ ;
STRING: '"' ([a-zA-Z0-9% ] | '\\' '"' | '\\' 'n')+ '"';
WHITESPACE: (' '|'\t'|'\r'|'\n')+ -> skip ;