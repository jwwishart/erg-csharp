using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApplication
{


    // TODO create customer parser exception functions instead of using ArgumentException
    // ???? why are we even thinking of using exceptions? return error codes instead or
    // ???? object fields with information about the error.
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("erg 0 (C) 2016 Justin Wishart\n");
            Console.WriteLine("Working Directory: " + Directory.GetCurrentDirectory());

            var code = File.ReadAllText(
                System.IO.Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "_temp",
                    "test.erg"));

            Console.WriteLine(code);

            // TOKENIZER
            //

            var tokens = Lex(code);
            foreach (var token in tokens) {
                Console.WriteLine(token.Type.ToString() + ": " + token.Value);
            }
            if (tokens.Count == 0) {
                Console.WriteLine("no tokens");
            }

            // Build AST
            ParseResult result;
            var context = new ParseContext();
            context.Tokens = tokens;
            context.Program = new ProgramNode();

            try {
                result = Parse(context);
            } catch (ArgumentException ex) {
                Console.WriteLine("Parser Error: " + ex.Message);
                result = new ParseResult { Success = false };
                throw;
            }

            if (result.Success) {
                Console.WriteLine("\nCompilation Done!");
            } else {
                Console.WriteLine("\nGet used to this... The program didn't compile :o(");
            }
        }

        public enum AstNodeType {
            Program,
            File,
            ExpressionStatement,
            BinaryExpression,
        }

        public abstract class AstNode 
        {
        }

        public class ProgramNode : AstNode 
        {

            public List<AstNode> Files = new List<AstNode>();
        }

        public class FileNode: AstNode {
            public List<AstNode> Body;
        }

        public class ExpressionStatement : AstNode {

        }

        public class BinaryExpression : AstNode {

            public AstNode Left = null;

            public AstNode Right = null;
        }

        public static ParseResult Parse(ParseContext context) {
            return Parse_Program(context);
        }

        public class ParseContext {
            public int Index = 0;
            public List<Token> Tokens;

            public ProgramNode Program = new ProgramNode();

            public List<AstNode> Scope {get;} = new List<AstNode>();

            public void PopScope() {
                Scope.RemoveAt(Scope.Count - 1);
            }

            public void PushScope(AstNode node) {
                Scope.Add(node);
            }

            public AstNode CurrentScope {
                get {
                    if (Scope.Count == 0) return Program;

                    return Scope[Scope.Count - 1];
                }
            }
        }

        public class ParseResult {
            public bool Success = true;
        }

        public static bool IsEof(ParseContext context) {
            return !(context.Tokens.Count > context.Index);
        }

        public static bool Accept(ParseContext context, TokenType type) {
            // Console.WriteLine("Accept: context.Index = " + context.Index);
            return context.Tokens[context.Index].Type == type;
        }

        public static bool Expect(ParseContext context, TokenType type) {
            if (context.Tokens[context.Index].Type != type) {
                // TODO is an exception here a good idea... 
                // I reallllyyyyyy don't like it...
                throw new ArgumentException(
                    "Expected token of type " + type.ToString() + 
                    " but got one of type " + context.Tokens[context.Index].Type.ToString());
            }

            return true;
        }

        public static ParseResult Parse_Program(ParseContext context) {
            context.Program = new ProgramNode();
            context.PushScope(context.Program);

            // TODO(jwwishart) need to ensure that all files are done before returning...
            // some might have been added during compilation
            var result = Parse_File(context);
            context.PopScope();
            return result;
        }

        public static ParseResult Parse_File(ParseContext context) {
            var file = new FileNode();
            context.Program.Files.Add(file);
            context.PushScope(file);

            while (context.Index <= context.Tokens.Count - 1) {
                var result = Parse_Statement(context);

                if (result.Success == false) {
                    context.PopScope();
                    return result;
                }
            }

            context.PopScope();
            return new ParseResult();
        }

        public static ParseResult Parse_Statement(ParseContext context) {
            var statement = new ExpressionStatement();
            context.PushScope(statement);

            // The destinction between a statement and an expression statement
            // is that the statement doesn't actually return anything... so 
            // the whole statement would not evaluate to something... but that is strange...
            // so why is everything not an expression? an Assignment? why not? it evaluates to
            // the lhs assigned value.
            if (IsEof(context)) {
                context.PopScope();

                return new ParseResult();
            }

            var currentToken = context.Tokens[context.Index];
            
            var result = Parse_Expression(context);
            context.PopScope();
            return result;
        }

        public static ParseResult Parse_Expression(ParseContext context) {
            // expression = ["+"|"-"] term {("+"|"-") term} .
            // TODO ? what type of expressions are there?
            // - binary expressions?
            // - ? unary expression? whaAAATTT?

            // TODO store the AST
            if (IsEof(context)) return new ParseResult();

            if (Accept(context, TokenType.Add) || Accept(context, TokenType.Subtract)) {
                context.Index++;
            
            }

            if (IsEof(context)) return new ParseResult();

            var term = Parse_Term(context);

            if (term.Success) {
                if (IsEof(context)) return new ParseResult();

                if (Accept(context, TokenType.Add) || Accept(context, TokenType.Add)) {
                    context.Index++;

                    if (IsEof(context)) return new ParseResult();

                    var term2 = Parse_Term(context);
                    if (term2.Success) {
                        return term2;
                    }

                    throw new ArgumentException("Parse Error: Expected infix operator provided but not subsequent term .");
                }

                return term;
            }

            throw new ArgumentException("Parse Error: Expected expression = [\"+\"|\"-\"] term {(\"+\"|\"-\") term} .");
        }

        public static ParseResult Parse_Term(ParseContext context) {
            // term = factor {("*"|"/") factor} .
            var factor = Parse_Factor(context);
            
            if (factor.Success == false) {
                return factor;
            }

            if (IsEof(context)) return new ParseResult();

            if (Accept(context, TokenType.Multiply)) {
                context.Index++;
                return Parse_Factor(context);
            }

            if (IsEof(context)) return new ParseResult();
            if (Accept(context, TokenType.Divide)) {
                context.Index++;
                return Parse_Factor(context);
            }

            // A factor might just be a number... there might be + or - after This
            // so this Term is done!!!
            return factor;
        }

        public static ParseResult Parse_Factor(ParseContext context) {
            // factor = ident | number | "(" expression ")" .

            // TODO identifiers
            // TODO Numbers and other primitive types
            // TODO expressions (recursive parenthesised expression? or 
            //   are the parenthesis optional?)

            // Numbers could be prefixed with - or +
            if (IsEof(context)) return new ParseResult();

            if (Accept(context, TokenType.Add) || Accept(context, TokenType.Subtract)) {
                context.Index++;

                Expect(context, TokenType.LiteralNumber);
            }

            if (IsEof(context)) return new ParseResult();

            if (Accept(context, TokenType.LiteralNumber)) {
                context.Index++;
                return new ParseResult();
            }

            throw new ArgumentException("Parse Error: Expected factor = ident | number | \"(\" expression \")\" .");
        }



        // Lexer
        //

        
        public enum TokenType {
            Unknown,
            Whitespace,
            
            // Literal Values
            LiteralNumber,

            // Operators
            Add,
            Subtract,
            Multiply,
            Divide
        }

        public class Token {
            public TokenType Type = TokenType.Unknown;
            public string Value = String.Empty;

            // TODO: include file, line and column stuff here
        }

        public static List<Token> Lex(string code) {
            var index = 0;
            var result = new List<Token>();

            while (true) {
                if (index > code.Length - 1) break;
                         
                switch (code[index]) {
                    case ' ':
                        // Basically ignore spaces for now
                        // TODO: return whitespace...
                        index++;
                        // result.Add(new Token { Type = TokenType.Whitespace});

                        continue;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':

                    case '9':
                        GetNumber(code, ref index, result);
                        continue;

                    case '+':
                        result.Add(new Token { Type = TokenType.Add });
                        index++;
                        continue;
                    case '-':
                        result.Add(new Token { Type = TokenType.Subtract });
                        index++;
                        continue;
                    case '*':
                        result.Add(new Token { Type = TokenType.Multiply });
                        index++;
                        continue;
                    case '/':
                        result.Add(new Token { Type = TokenType.Divide });
                        index++;
                        continue;
                }

                throw new ArgumentException("This token, I do not know");
            }

            return result;
        }

        private static void GetNumber(string code, ref int index, IList<Token> results) {
            var numberToken = new Token { Type = TokenType.LiteralNumber };

            while (true) {
                if (index > code.Length - 1) break;

                switch (code[index]) {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        // TODO PERF string builder in context that 
                        // retains it's capacity and is re-used 
                        // instead of repeated string concatention
                        numberToken.Value += code[index];
                        index++;
                        continue;
                }

                break;
            }

            results.Add(numberToken);
        }
    }
}
