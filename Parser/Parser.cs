using System;
using System.Collections.Generic;


namespace Parser
{
    public enum Symbol
    {
        INVALID, ID, REAL, WS,
        LPAREN, RPAREN, EQUALS,
        OP_PLUS, OP_MINUS, OP_MULTIPLY, OP_DIVIDE, OP_EXPONENT
    };

    public class Node
    {
        Symbol symbol = Symbol.INVALID;
        public String lexeme = "";
        List<Node> children = new List<Node>();

        public Symbol Symbol
        {
            get
            {
                return symbol;
            }
            set
            {
                symbol = value;
            }
        }

        public Node(Symbol symbol, String lexeme, params Node[] children)
        {
            this.symbol = symbol;
            this.lexeme = lexeme;
            this.children = new List<Node>(children);
        }

        public void Dump(System.IO.TextWriter output, String prefix = "")
        {
            String symbolStr = symbol.ToString();
            while (symbolStr.Length < 15)
            {
                symbolStr += " ";
            }
            output.WriteLine($"{prefix}{symbolStr}\t{lexeme}");
            foreach (Node child in children)
            {
                child.Dump(output, prefix + "    ");
            }
        }
    }


    /** 
     * Build a parser that supports assignments
     *     var = expr
     *
     * Tokens:
     *      ID:  Any letter followed be a sequence of letters and numbers
     *      REAL: An optional sign followed by a sequence of digits, optionally with single decimal point. 
     *      WS:  Whitespace (no tokens generated, this is skipped)
     *      LPAREN, RPAREN, EQUALS:  (, ), and = literals
     *      OP_PLUS, OP_MINUS: + and - literals
     *      OP_MULTIPLY, OP_DIVIDE:  * and / literals
     *      OP_EXPONENT: ** literal (x**2 is "x squared"). 
     *Grammar:
     *      <stmt> ::= <assign> | <expr>
     *      <assign> ::= ID = <expr>
     *      <expr> ::= <term> | <term> + <expr> | <term> - <expr>
     *      <term> ::= <factor> | <factor> * <term> | <factor> / <term>
     *      <factor> ::= <base>**<factor> | <base>
     *      <base> := ID | NUM |  (<expr>)
     */
    public class Parser
    {

        String text = "";
        int pos = 0;
        Node token;
        public int line;
        int linePosition = 0;

        public Parser(String source, int line)
        {
            text = source;
            pos = 0;
            this.line = line;
        }

        public int getCol()
        {
            return pos - linePosition + 1;
        }

        public void NextToken()
        {
            int state = 0;
            System.Text.StringBuilder lexeme = new System.Text.StringBuilder();
            token = null;

            while (pos <= text.Length)
            {
                char c = pos == text.Length ? '$' : text[pos];
                switch (state)
                {
                    case 0:
                        if (char.IsWhiteSpace(c))
                        {
                            pos += 1;
                            if (c == '\n')
                            {
                                line++;
                                linePosition = pos;
                            }
                        }
                        else if (c >= '0' && c <= '9')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 1;
                        }
                        else if (c == '+')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 3;
                        }
                        else if (c == '-')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 4;
                        }
                        else if (c == '*')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 5;
                        }
                        else if (c == '/')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            token = new Node(Symbol.OP_DIVIDE, lexeme.ToString());
                            return;
                        }
                        else if (c == '=')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            token = new Node(Symbol.EQUALS, lexeme.ToString());
                            return;
                        }
                        else if (c == '(')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            token = new Node(Symbol.LPAREN, lexeme.ToString());
                            return;
                        }
                        else if (c == ')')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            token = new Node(Symbol.RPAREN, lexeme.ToString());
                            return;
                        }
                        else if (char.IsLetter(c))
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 9;
                        }
                        else
                        {
                            token = pos == text.Length ? null : new Node(Symbol.INVALID, "" + c);
                            return;
                        }
                        break;
                    case 1:
                        if (char.IsDigit(c))
                        {
                            pos += 1;
                            lexeme.Append(c);
                        }
                        else if (c == '.')
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 2;
                        }
                        else
                        {
                            token = new Node(Symbol.REAL, lexeme.ToString());
                            return;
                        }
                        break;
                    case 2:
                        if (char.IsDigit(c))
                        {
                            pos += 1;
                            lexeme.Append(c);
                        }
                        else
                        {
                            token = new Node(Symbol.REAL, lexeme.ToString());
                            return;
                        }
                        break;
                    case 3:
                    case 4:
                        if (char.IsDigit(c))
                        {
                            pos += 1;
                            lexeme.Append(c);
                            state = 1;
                        }
                        else
                        {
                            token = new Node(state == 3 ? Symbol.OP_PLUS : Symbol.OP_MINUS, lexeme.ToString());
                            return;
                        }
                        break;
                    case 5:
                        if (c == '*')
                        {
                            pos++;
                            lexeme.Append(c);
                            token = new Node(Symbol.OP_EXPONENT, lexeme.ToString());
                        }
                        else
                        {
                            token = new Node(Symbol.OP_MULTIPLY, lexeme.ToString());
                        }
                        return;
                    case 9:
                        if (char.IsLetter(c) || char.IsDigit(c))
                        {
                            pos += 1;
                            lexeme.Append(c);
                        }
                        else
                        {
                            token = new Node(Symbol.ID, lexeme.ToString());
                            return;
                        }
                        break;
                }
            }
        }

        public static void Main()
        {
            int line = 1;
            String source = Console.ReadLine();
            while (source != null)
            {
                Parser p = new Parser(source, line++);

                p.NextToken();
                while (p.token != null && p.token.Symbol != Symbol.INVALID)
                {
                    p.token.Dump(System.Console.Out);
                    p.NextToken();
                }
                if (p.token != null && p.token.Symbol == Symbol.INVALID)
                {
                    Console.Out.WriteLine($"Invalid character '{p.token.lexeme}' at line {p.line} column {p.getCol()}");
                    return;
                }

                source = Console.ReadLine();
            }
        }
    }
}

