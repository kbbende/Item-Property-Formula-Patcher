using System.Globalization;

namespace ItemPropertyFormulaPatcher.Formula;

internal enum TokenKind { Number, Identifier, Symbol, End }
internal readonly record struct Token(TokenKind Kind, string Text, int Position, double Number = 0);

internal static class FormulaLexer
{
    public static IReadOnlyList<Token> Tokenize(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new FormulaException("Formula is empty.");
        if (source.Length > 8192) throw new FormulaException("Formula exceeds 8192 characters.");
        var tokens = new List<Token>();
        var i = 0;
        while (i < source.Length)
        {
            if (char.IsWhiteSpace(source[i])) { i++; continue; }
            var start = i;
            var c = source[i];
            if (char.IsAsciiDigit(c) || c == '.')
            {
                while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                if (i < source.Length && source[i] == '.')
                {
                    i++;
                    while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                }
                if (i < source.Length && source[i] is 'e' or 'E')
                {
                    i++;
                    if (i < source.Length && source[i] is '+' or '-') i++;
                    while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                }
                var text = source[start..i];
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
                    throw new FormulaException($"Invalid numeric literal '{text}' at position {start}.");
                tokens.Add(new Token(TokenKind.Number, text, start, number));
            }
            else if (char.IsAsciiLetter(c) || c == '_')
            {
                i++;
                while (i < source.Length && (char.IsAsciiLetterOrDigit(source[i]) || source[i] == '_')) i++;
                tokens.Add(new Token(TokenKind.Identifier, source[start..i], start));
            }
            else
            {
                var pair = i + 1 < source.Length ? source.Substring(i, 2) : "";
                if (pair is ">=" or "<=" or "==" or "!=" or "&&" or "||") i += 2;
                else if ("+-*/%^><!(),".Contains(c)) i++;
                else throw new FormulaException($"Unexpected character '{c}' at position {i}.");
                tokens.Add(new Token(TokenKind.Symbol, source[start..i], start));
            }
            if (tokens.Count > 1024) throw new FormulaException("Formula exceeds 1024 tokens.");
        }
        tokens.Add(new Token(TokenKind.End, "", source.Length));
        return tokens;
    }
}
