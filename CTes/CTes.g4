grammar CTes;

/*
 * Parser Rules
 */

program: (includeLibrary | externFunctionDeclaration | externVariableDeclaration | constVariableDeclaration | functionDefinition | statement)* ;
includeLibrary: 'include' lib=WORD ';' ;
externFunctionDeclaration: 'extern' returnType=typeIdentifier name=WORD parameters ';' ;
externVariableDeclaration: 'extern' type=typeIdentifier name=WORD ';' ;
constVariableDeclaration: 'const' type=typeIdentifier name=WORD '=' value ';' ;
functionDefinition: returnType=typeIdentifier name=WORD parameters '{' statement* returnStatement? '}' ;
statement: functionCall ';' ;
returnStatement: 'return' expression ';' ;
expression: functionCall | add | dereference | value ;
add: lhs=value '+' rhs=value ;
dereference: '*' expression ;
functionCall: function=valueIdentifier arguments ;
arguments: '(' (args+=expression (',' args+=expression)* ','?)? ')' ;
parameters: '(' (params+=parameter (',' params+=parameter)* ','?)? ')' ;
parameter
    : type=typeIdentifier name=WORD
    | varArg='...'
    ;
value: valueIdentifier | number | string ;
valueIdentifier: WORD ;
number: decimalNumber | hexNumber ;
decimalNumber: digits=NUMBER ;
hexNumber: digits=HEX_NUMBER ;
typeIdentifier: name=WORD (pointers+='*')* ;
string: '"' CHARACTER? '"' ;

/*
 * Lexer Rules
 */

WORD: [a-zA-Z_][a-zA-Z0-9_]* ;
NUMBER: [0-9]+ ;
HEX_NUMBER: '0x'[0-9a-fA-F]+ ;
CHARACTER: [a-zA-Z0-9%\\]+ ;
WHITESPACE: (' '|'\t'|'\r'|'\n')+ -> skip ;