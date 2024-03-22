grammar CTes;

/*
 * Parser Rules
 */

file: (functionDefinition | statement)* ;
functionDefinition: returnType=WORD functionName=WORD parameters '{' statement* return? '}' ;
statement: functionCall ';' ;
return: 'return' expression ';' ;
expression: WORD | NUMBER | expression '+' expression | functionCall ;
functionCall: WORD arguments ;
arguments: '(' (expression (',' expression)* ','?)? ')' ;
parameters: '(' (WORD WORD (',' WORD WORD)* ','?)? ')' ;

/*
 * Lexer Rules
 */

WORD: [a-zA-Z]+ ;
NUMBER: [0-9]+ ;
WHITESPACE: (' '|'\t'|'\r'|'\n')+ -> skip ;