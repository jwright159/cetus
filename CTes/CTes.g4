grammar CTes;

/*
 * Parser Rules
 */

program: (includeLibrary | externFunction | functionDefinition | statement)* ;
includeLibrary: 'include' lib=WORD ';' ;
externFunction: 'extern' returnType=typeIdentifier functionName=WORD parameters ';' ;
functionDefinition: returnType=typeIdentifier functionName=WORD parameters '{' statement* returnStatement? '}' ;
statement: functionCall ';' ;
returnStatement: 'return' expression ';' ;
expression: functionCall | add | value ;
add: lhs=value '+' rhs=value ;
functionCall: function=valueIdentifier arguments ;
arguments: '(' (args+=expression (',' args+=expression)* ','?)? ')' ;
parameters: '(' (params+=parameter (',' params+=parameter)* ','?)? ')' ;
parameter
    : type=typeIdentifier name=WORD
    | varArg='...'
    ;
value: valueIdentifier | number | string ;
valueIdentifier: WORD ;
number: NUMBER ;
typeIdentifier: name=WORD (pointers+='*')* ;
string: '"' CHARACTER? '"' ;

/*
 * Lexer Rules
 */

WORD: [a-zA-Z][a-zA-Z0-9]* ;
NUMBER: [0-9]+ ;
CHARACTER: [a-zA-Z0-9%\\]+ ;
WHITESPACE: (' '|'\t'|'\r'|'\n')+ -> skip ;