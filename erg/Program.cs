using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("erg 0 (C) 2016 Justin Wishart");
            Console.WriteLine(Directory.GetCurrentDirectory());

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
                Console.WriteLine(token.Type.ToString());
            }
            if (tokens.Count == 0) {
                Console.WriteLine("no tokens");
            }

            // Build AST
            var ast = Parse(tokens);
        }

        public abstract class AstNode 
        {
            public abstract AstNodeType Type { get; }
        }

        public class ProgramNode : AstNode 
        {
            public override AstNodeType Type => AstNodeType.Program;

            public List<AstNode> Body;
        }

        public class ExpressionStatement : AstNode {
            public override AstNodeType Type => AstNodeType.ExpressionStatement;

            public AstNode Left;

            public AstNode Right;
        }



        public enum AstNodeType {
            Program,
            ExpressionStatement,
            BinaryExpression,
        }

        public static ParseResult Parse(List<Token> tokens) {
            var context = new ParseContext();

            while (context.Index > context.Tokens.Count - 1) {
                var result = Parse_Statement(context);
                if (result.Success == false) {
                    return result;
                }
            }

            return new ParseResult();
        }

        public class ParseContext {
            public int Index = 0;
            public List<Token> Tokens;

            public ProgramNode Program = new ProgramNode();

            public AstNode Current = null;
        }

        public class ParseResult {
            public bool Success = true;
        }

        public static bool Accept(ParseContext context, TokenType type) {
            return context.Tokens[context.Index].Type == type;
        }

        public static bool Expect(ParseContext context, TokenType type) {
            if (context.Tokens[context.Index].Type != type) {
                return false;
            }

            return true;
        }

        public static ParseResult Parse_Statement(ParseContext context) {
            context.Program = new ProgramNode();

            // The destinction between a statement and an expression statement
            // is that the statement doesn't actually return anything... so 
            // the whole statement would not evaluate to something... but that is strange...
            // so why is everything not an expression? an Assignment? why not? it evaluates to
            // the lhs assigned value.
            var currentToken = context.Tokens[context.Index];

            return Parse_Expression(context);
        }

        public static ParseResult Parse_Expression(ParseContext context) {
            // expression = ["+"|"-"] term {("+"|"-") term} .
            return Parse_Term(context);
        }

        public static ParseResult Parse_Term(ParseContext context) {
            // term = factor {("*"|"/") factor} .
            return Parse_Factor(context);
        }

        public static ParseResult Parse_Factor(ParseContext context) {
           // factor = ident | number | "(" expression ")" .
           return new ParseResult();
        }

        public enum TokenType {
            Unknown,

            // Literal Values
            LiteralNumber,

            // Operators
            Plus
        }

        public class Token {
            public TokenType Type = TokenType.Unknown;
            public string Value = String.Empty;
        }

        public static List<Token> Lex(string code) {
            var index = 0;
            var result = new List<Token>();

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
                        // TODO inefficient :oS
                        result.Add(new Token { Type = TokenType.LiteralNumber, Value = code[index].ToString() });
                        index++;
                        continue;

                    case '+':
                        result.Add(new Token { Type = TokenType.Plus });
                        index++;
                        continue;
                }

                throw new ArgumentException("This token, I do not know");
            }

            return result;
        }
    }
}
