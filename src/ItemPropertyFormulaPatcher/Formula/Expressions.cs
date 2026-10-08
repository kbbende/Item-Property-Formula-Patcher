namespace ItemPropertyFormulaPatcher.Formula;

public abstract class Expression
{
    public double Evaluate(EvaluationContext context)
    {
        var result = EvaluateCore(context);
        if (!double.IsFinite(result)) throw new FormulaException("Expression produced NaN or infinity.");
        return result;
    }

    protected abstract double EvaluateCore(EvaluationContext context);
    protected static bool Truth(double value) => value != 0;
}

internal sealed class NumberExpression(double value) : Expression
{
    protected override double EvaluateCore(EvaluationContext context) => value;
}

internal sealed class VariableExpression(string name) : Expression
{
    protected override double EvaluateCore(EvaluationContext context) => context.Get(name);
}

internal sealed class UnaryExpression(string op, Expression operand) : Expression
{
    protected override double EvaluateCore(EvaluationContext context)
    {
        var value = operand.Evaluate(context);
        return op switch { "+" => value, "-" => -value, "!" => Truth(value) ? 0 : 1, _ => throw new FormulaException("Invalid unary operator.") };
    }
}

internal sealed class BinaryExpression(string op, Expression left, Expression right) : Expression
{
    protected override double EvaluateCore(EvaluationContext context)
    {
        var a = left.Evaluate(context);
        if (op == "&&" && !Truth(a)) return 0;
        if (op == "||" && Truth(a)) return 1;
        var b = right.Evaluate(context);
        if (op is "/" or "%" && b == 0) throw new FormulaException("Division or remainder by zero.");
        return op switch
        {
            "+" => a + b, "-" => a - b, "*" => a * b, "/" => a / b, "%" => a % b, "^" => Math.Pow(a, b),
            ">" => a > b ? 1 : 0, ">=" => a >= b ? 1 : 0, "<" => a < b ? 1 : 0, "<=" => a <= b ? 1 : 0,
            "==" => a == b ? 1 : 0, "!=" => a != b ? 1 : 0,
            "&&" => Truth(b) ? 1 : 0, "||" => Truth(b) ? 1 : 0,
            _ => throw new FormulaException("Invalid binary operator.")
        };
    }
}

internal sealed class FunctionExpression(string name, IReadOnlyList<Expression> arguments) : Expression
{
    public static void Validate(string name, int count)
    {
        var valid = name switch
        {
            "min" or "max" => count >= 1,
            "clamp" or "if" => count == 3,
            "round" or "floor" or "ceil" or "log" => count is 1 or 2,
            "pow" => count == 2,
            "abs" or "sqrt" or "log10" or "exp" or "sign" or "trunc" => count == 1,
            _ => throw new FormulaException($"Unknown function '{name}'.")
        };
        if (!valid) throw new FormulaException($"Invalid argument count for '{name}': {count}.");
    }

    protected override double EvaluateCore(EvaluationContext context)
    {
        // Evaluate only the selected branch. Logical operators also short-circuit.
        if (name == "if") return arguments[Truth(arguments[0].Evaluate(context)) ? 1 : 2].Evaluate(context);
        var values = arguments.Select(a => a.Evaluate(context)).ToArray();
        var x = values[0];
        if (name is "round" or "floor" or "ceil")
        {
            var step = values.Length == 2 ? values[1] : 1;
            if (step <= 0) throw new FormulaException("Rounding step must be positive.");
            var scaled = x / step;
            if (!double.IsFinite(scaled)) throw new FormulaException("Rounding overflow.");
            return (name switch
            {
                "round" => Math.Round(scaled, MidpointRounding.AwayFromZero),
                "floor" => Math.Floor(scaled),
                _ => Math.Ceiling(scaled)
            }) * step;
        }
        if (name == "clamp" && values[1] > values[2]) throw new FormulaException("Clamp minimum exceeds maximum.");
        return name switch
        {
            "min" => values.Min(), "max" => values.Max(), "clamp" => Math.Clamp(x, values[1], values[2]),
            "abs" => Math.Abs(x), "sqrt" => Math.Sqrt(x), "pow" => Math.Pow(x, values[1]),
            "log" => values.Length == 1 ? Math.Log(x) : Math.Log(x, values[1]), "log10" => Math.Log10(x),
            "exp" => Math.Exp(x), "sign" => Math.Sign(x), "trunc" => Math.Truncate(x),
            _ => throw new FormulaException($"Unknown function '{name}'.")
        };
    }
}
