using System.Text.RegularExpressions;

namespace SciSolve.Helpers
{
    public delegate double MathFunc(double x);

    public static class FunctionParser
    {
        public static MathFunc? Parse(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return null;
            try
            {
                expr = expr.Trim()
                           .Replace("**", "^")
                           .Replace("pi", $"{Math.PI}")
                           .Replace(" e ", $" {Math.E} ")
                           .Replace("(e)", $"({Math.E})")
                           .Replace("^e)", $"^{Math.E})");
                // protect 'exp' before replacing bare 'e'
                var tokens = Tokenize(expr);
                int pos = 0;
                var node = ParseExpr(tokens, ref pos);
                return x => node.Eval(x);
            }
            catch { return null; }
        }

        // Tokenizer
        private enum TT { Num, Id, Plus, Minus, Mul, Div, Pow, LP, RP, EOF }
        private record Token(TT Type, string Val);

        private static List<Token> Tokenize(string s)
        {
            var list = new List<Token>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                {
                    int st = i;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' ||
                        ((s[i] == 'e' || s[i] == 'E') && i + 1 < s.Length &&
                         (s[i + 1] == '+' || s[i + 1] == '-' || char.IsDigit(s[i + 1])))))
                        i++;
                    if (i < s.Length && (s[i] == '+' || s[i] == '-') &&
                        i > st && (s[i - 1] == 'e' || s[i - 1] == 'E')) i++;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                    list.Add(new Token(TT.Num, s[st..i]));
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int st = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    list.Add(new Token(TT.Id, s[st..i]));
                    continue;
                }
                switch (c)
                {
                    case '+': list.Add(new Token(TT.Plus, "+")); break;
                    case '-': list.Add(new Token(TT.Minus, "-")); break;
                    case '*': list.Add(new Token(TT.Mul, "*")); break;
                    case '/': list.Add(new Token(TT.Div, "/")); break;
                    case '^': list.Add(new Token(TT.Pow, "^")); break;
                    case '(': list.Add(new Token(TT.LP, "(")); break;
                    case ')': list.Add(new Token(TT.RP, ")")); break;
                    default: throw new Exception($"Unknown char '{c}'");
                }
                i++;
            }
            list.Add(new Token(TT.EOF, ""));
            return list;
        }

        // Parser
        private abstract class Node { public abstract double Eval(double x); }

        private class NumNode(double v) : Node
        { public override double Eval(double x) => v; }

        private class VarNode : Node
        { public override double Eval(double x) => x; }

        private class NegNode(Node n) : Node
        { public override double Eval(double x) => -n.Eval(x); }

        private class BinNode(char op, Node l, Node r) : Node
        {
            public override double Eval(double x) => op switch
            {
                '+' => l.Eval(x) + r.Eval(x),
                '-' => l.Eval(x) - r.Eval(x),
                '*' => l.Eval(x) * r.Eval(x),
                '/' => l.Eval(x) / r.Eval(x),
                '^' => Math.Pow(l.Eval(x), r.Eval(x)),
                _ => throw new Exception($"Unknown op {op}")
            };
        }

        private class FuncNode(string name, Node arg) : Node
        {
            public override double Eval(double x)
            {
                double v = arg.Eval(x);
                return name.ToLower() switch
                {
                    "sin" => Math.Sin(v),
                    "cos" => Math.Cos(v),
                    "tan" => Math.Tan(v),
                    "exp" => Math.Exp(v),
                    "log" => Math.Log(v),
                    "log10" => Math.Log10(v),
                    "log2" => Math.Log2(v),
                    "sqrt" => Math.Sqrt(v),
                    "abs" => Math.Abs(v),
                    "sinh" => Math.Sinh(v),
                    "cosh" => Math.Cosh(v),
                    "tanh" => Math.Tanh(v),
                    "asin" => Math.Asin(v),
                    "acos" => Math.Acos(v),
                    "atan" => Math.Atan(v),
                    "ceil" => Math.Ceiling(v),
                    "floor" => Math.Floor(v),
                    _ => throw new Exception($"Unknown function '{name}'")
                };
            }
        }

        private static Node ParseExpr(List<Token> t, ref int pos)
        {
            var left = ParseTerm(t, ref pos);
            while (pos < t.Count && (t[pos].Type == TT.Plus || t[pos].Type == TT.Minus))
            {
                char op = t[pos].Type == TT.Plus ? '+' : '-'; pos++;
                left = new BinNode(op, left, ParseTerm(t, ref pos));
            }
            return left;
        }

        private static Node ParseTerm(List<Token> t, ref int pos)
        {
            var left = ParsePow(t, ref pos);
            while (pos < t.Count && (t[pos].Type == TT.Mul || t[pos].Type == TT.Div))
            {
                char op = t[pos].Type == TT.Mul ? '*' : '/'; pos++;
                left = new BinNode(op, left, ParsePow(t, ref pos));
            }
            return left;
        }

        private static Node ParsePow(List<Token> t, ref int pos)
        {
            var b = ParseUnary(t, ref pos);
            if (pos < t.Count && t[pos].Type == TT.Pow) { pos++; return new BinNode('^', b, ParsePow(t, ref pos)); }
            return b;
        }

        private static Node ParseUnary(List<Token> t, ref int pos)
        {
            if (pos < t.Count && t[pos].Type == TT.Minus) { pos++; return new NegNode(ParseUnary(t, ref pos)); }
            if (pos < t.Count && t[pos].Type == TT.Plus) { pos++; return ParseUnary(t, ref pos); }
            return ParsePrimary(t, ref pos);
        }

        private static Node ParsePrimary(List<Token> t, ref int pos)
        {
            var tok = t[pos];
            if (tok.Type == TT.Num)
            {
                pos++;
                return new NumNode(double.Parse(tok.Val,
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            if (tok.Type == TT.Id)
            {
                string name = tok.Val.ToLower(); pos++;
                if (name == "pi") return new NumNode(Math.PI);
                if (name == "e") return new NumNode(Math.E);
                if (name == "x") return new VarNode();
                if (pos < t.Count && t[pos].Type == TT.LP)
                {
                    pos++;
                    var arg = ParseExpr(t, ref pos);
                    if (t[pos].Type != TT.RP) throw new Exception("Expected ')'");
                    pos++;
                    return new FuncNode(name, arg);
                }
                return new VarNode();
            }
            if (tok.Type == TT.LP)
            {
                pos++;
                var inner = ParseExpr(t, ref pos);
                if (t[pos].Type != TT.RP) throw new Exception("Expected ')'");
                pos++;
                return inner;
            }
            throw new Exception($"Unexpected token '{tok.Val}'");
        }
    }
}

