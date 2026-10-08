namespace ItemPropertyFormulaPatcher.Formula;

public sealed class FormulaParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;
    private int _depth;

    private FormulaParser(string source) => _tokens = FormulaLexer.Tokenize(source);
    private Token Current => _tokens[_position];

    public static Expression Parse(string source)
    {
        var parser = new FormulaParser(source);
        var expression = parser.ParseBinary(0);
        if (parser.Current.Kind != TokenKind.End) throw parser.Error("Unexpected token");
        return expression;
    }

    private FormulaException Error(string message) => new($"{message} '{Current.Text}' at position {Current.Position}.");
    private bool Take(string text)
    {
        if (Current.Text != text) return false;
        _position++;
        return true;
    }

    private static int Precedence(string op) => op switch
    {
        "||" => 1, "&&" => 2, "==" or "!=" => 3, ">" or ">=" or "<" or "<=" => 4,
        "+" or "-" => 5, "*" or "/" or "%" => 6, "^" => 8, _ => -1
    };

    private Expression ParseBinary(int minimum)
    {
        if (++_depth > 64) throw Error("Formula nesting exceeds 64 levels near");
        try
        {
            Expression left;
            if (Current.Text is "+" or "-" or "!")
            {
                var op = Current.Text;
                _position++;
                left = new UnaryExpression(op, ParseBinary(7));
            }
            else left = ParsePrimary();
            while (Precedence(Current.Text) >= minimum)
            {
                var op = Current.Text;
                var precedence = Precedence(op);
                _position++;
                left = new BinaryExpression(op, left, ParseBinary(op == "^" ? precedence : precedence + 1));
            }
            return left;
        }
        finally { _depth--; }
    }

    private Expression ParsePrimary()
    {
        var token = Current;
        if (token.Kind == TokenKind.Number) { _position++; return new NumberExpression(token.Number); }
        if (Take("("))
        {
            var expression = ParseBinary(0);
            if (!Take(")")) throw Error("Expected closing parenthesis before");
            return expression;
        }
        if (token.Kind != TokenKind.Identifier) throw Error("Expected an expression before");
        _position++;
        if (!Take("("))
        {
            if (!EvaluationContext.IsKnownVariable(token.Text)) throw new FormulaException($"Unknown variable '{token.Text}' at position {token.Position}.");
            return new VariableExpression(token.Text);
        }
        var arguments = new List<Expression>();
        if (!Take(")"))
        {
            do { arguments.Add(ParseBinary(0)); } while (Take(","));
            if (!Take(")")) throw Error("Expected closing parenthesis before");
        }
        var name = token.Text.ToLowerInvariant();
        FunctionExpression.Validate(name, arguments.Count);
        return new FunctionExpression(name, arguments);
    }
}
