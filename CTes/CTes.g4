grammar CTes;

/*
 * Parser Rules
 */

program: (functionDefinition | statement)* ;
functionDefinition: returnType=typeIdentifier functionName=WORD parameters '{' statement* returnStatement? '}' ;
statement: functionCall ';' ;
returnStatement: 'return' expression ';' ;
expression: functionCall | add | value ;
add: lhs=value '+' rhs=value ;
functionCall: function=valueIdentifier arguments ;
arguments: '(' (args+=expression (',' args+=expression)* ','?)? ')' ;
parameters: '(' (types+=typeIdentifier args+=WORD (',' types+=typeIdentifier args+=WORD)* ','?)? ')' ;
value: valueIdentifier | number | string ;
valueIdentifier: WORD ;
number: NUMBER ;
typeIdentifier: WORD ;
string: '"' CHARACTER? '"' ;

/*
 * Lexer Rules
 */

WORD: [a-zA-Z]+ ;
NUMBER: [0-9]+ ;
CHARACTER: [a-zA-Z0-9%\\]+ ;
WHITESPACE: (' '|'\t'|'\r'|'\n')+ -> skip ;