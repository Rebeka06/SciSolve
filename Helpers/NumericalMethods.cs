using SciSolve.Models;

namespace SciSolve.Helpers
{
    public static class NumericalMethods
    {
        // 1. Newton-Raphson
        public static SolveResult Newton(MathFunc f, MathFunc df,
                                          double x0, double tol, int maxIter)
        {
            var res = new SolveResult();
            res.Log.AddRange(new[]
            {
                "Newton-Raphson Method",
                new string('=', 52),
                "Formula:  x_new = x − f(x) / f'(x)",
                $"x\u2080 = {x0}   Tolerance: {tol}   Max iterations: {maxIter}",
                new string('=', 52)
            });

            double x = x0;
            for (int i = 0; i < maxIter; i++)
            {
                double fx = f(x);
                double dfx = df(x);
                if (Math.Abs(dfx) < 1e-15)
                {
                    res.ErrorMsg = "f'(x) \u2248 0 — cannot continue (division by zero).";
                    res.Root = x; return res;
                }
                double xNew = x - fx / dfx;
                double err = Math.Abs(xNew - x);

                res.Steps.Add(new IterationStep
                {
                    Iteration = i + 1,
                    X = xNew,
                    FX = f(xNew).ToString("F10"),
                    Error = err
                });
                res.Errors.Add(err);
                res.Log.Add($"Iter {i + 1,3}: x={x,16:F10}  f(x)={fx,14:F8}  f'(x)={dfx,14:F8}");
                res.Log.Add($"         x_new = {x:F8} \u2212 ({fx:E4}/{dfx:E4}) = {xNew:F10}   |err|={err:E3}");

                if (err < tol)
                {
                    res.Log.Add($"\n\u2713 Converged in {i + 1} iterations.  Root \u2248 {xNew:F10}");
                    res.Root = xNew; res.Converged = true; return res;
                }
                x = xNew;
                res.Log.Add("");
            }
            res.Log.Add($"\n\u26a0 Did not converge in {maxIter} iterations.  Best: {x:F10}");
            res.Root = x; return res;
        }

        // 2. Secant
        public static SolveResult Secant(MathFunc f,
                                          double x0, double x1,
                                          double tol, int maxIter)
        {
            var res = new SolveResult();
            res.Log.AddRange(new[]
            {
                "Secant Method",
                new string('=', 52),
                "Formula:  x\u2082 = x\u2081 \u2212 f(x\u2081)\u00b7(x\u2081\u2212x\u2080) / (f(x\u2081)\u2212f(x\u2080))",
                $"x\u2080={x0}   x\u2081={x1}   Tolerance:{tol}",
                new string('=', 52)
            });

            double xa = x0, xb = x1, x2 = x1;
            for (int i = 0; i < maxIter; i++)
            {
                double fxa = f(xa), fxb = f(xb);
                double denom = fxb - fxa;
                if (Math.Abs(denom) < 1e-15)
                {
                    res.ErrorMsg = "f(x\u2081)\u2212f(x\u2080) \u2248 0 — division by zero.";
                    res.Root = xb; return res;
                }
                x2 = xb - fxb * (xb - xa) / denom;
                double err = Math.Abs(x2 - xb);
                res.Steps.Add(new IterationStep { Iteration = i + 1, X = x2, FX = f(x2).ToString("F10"), Error = err });
                res.Errors.Add(err);
                res.Log.Add($"Iter {i + 1,3}: x\u2080={xa,12:F6}  x\u2081={xb,12:F6}  f\u2080={fxa,12:F6}  f\u2081={fxb,12:F6}");
                res.Log.Add($"         x\u2082={x2:F10}   |err|={err:E3}");
                if (err < tol)
                {
                    res.Log.Add($"\n\u2713 Converged in {i + 1} iterations.  Root \u2248 {x2:F10}");
                    res.Root = x2; res.Converged = true; return res;
                }
                xa = xb; xb = x2;
                res.Log.Add("");
            }
            res.Log.Add($"\n\u26a0 Did not converge.  Best: {x2:F10}");
            res.Root = x2; return res;
        }

        // 3. Bisection
        public static SolveResult Bisection(MathFunc f,
                                             double a, double b,
                                             double tol, int maxIter)
        {
            var res = new SolveResult();
            if (f(a) * f(b) > 0)
            {
                res.ErrorMsg = "f(a)\u00b7f(b) > 0 — root not guaranteed in [a,b].  Change interval.";
                return res;
            }
            res.Log.AddRange(new[]
            {
                "Bisection Method",
                new string('=', 52),
                "Formula:  c = (a+b)/2  \u2192  narrow bracket each step",
                $"[a,b] = [{a},{b}]   Tolerance:{tol}",
                new string('=', 52)
            });

            double c = a;
            for (int i = 0; i < maxIter; i++)
            {
                c = (a + b) / 2.0;
                double fc = f(c);
                double err = Math.Abs(b - a) / 2.0;
                res.Steps.Add(new IterationStep { Iteration = i + 1, X = c, FX = fc.ToString("F10"), Error = err });
                res.Errors.Add(err);
                res.Log.Add($"Iter {i + 1,3}: a={a,12:F6}  b={b,12:F6}  c={c,14:F10}");
                res.Log.Add($"         f(c)={fc,12:F8}   interval={err:E3}");
                if (Math.Abs(fc) < tol || err < tol)
                {
                    res.Log.Add($"\n\u2713 Converged in {i + 1} iterations.  Root \u2248 {c:F10}");
                    res.Root = c; res.Converged = true; return res;
                }
                if (f(a) * fc < 0) { b = c; res.Log.Add($"         \u2192 f(a)\u00b7f(c)<0  new b={b:F6}"); }
                else { a = c; res.Log.Add($"         \u2192 f(a)\u00b7f(c)\u22650  new a={a:F6}"); }
                res.Log.Add("");
            }
            res.Log.Add($"\n\u26a0 Did not converge.  Best: {c:F10}");
            res.Root = c; return res;
        }

        // 4. Fixed Point
        public static SolveResult FixedPoint(MathFunc g,
                                              double x0, double tol, int maxIter)
        {
            var res = new SolveResult();
            res.Log.AddRange(new[]
            {
                "Fixed Point Iteration",
                new string('=', 52),
                "Formula:  x_new = g(x)   until |x_new\u2212x| < tol",
                $"x\u2080={x0}   Tolerance:{tol}",
                "Convergence requires |g'(x)| < 1 near the root.",
                new string('=', 52)
            });
            double x = x0;
            for (int i = 0; i < maxIter; i++)
            {
                double xNew;
                try { xNew = g(x); }
                catch (Exception ex)
                { res.ErrorMsg = $"Eval error: {ex.Message}"; res.Root = x; return res; }

                double err = Math.Abs(xNew - x);
                res.Steps.Add(new IterationStep { Iteration = i + 1, X = xNew, FX = "", Error = err });
                res.Errors.Add(err);
                res.Log.Add($"Iter {i + 1,3}: x={x,16:F10}   g(x)={xNew,16:F10}   |err|={err:E3}");
                if (err < tol)
                {
                    res.Log.Add($"\n\u2713 Converged in {i + 1} iterations.  Fixed point \u2248 {xNew:F10}");
                    res.Root = xNew; res.Converged = true; return res;
                }
                x = xNew;
            }
            res.Log.Add($"\n\u26a0 Did not converge.  Best: {x:F10}");
            res.Root = x; return res;
        }

        // 5. Gauss-Seidel
        public static GaussResult GaussSeidel(double[,] A, double[] b,
                                               double tol, int maxIter)
        {
            int n = b.Length;
            var res = new GaussResult();
            res.Log.AddRange(new[]
            {
                "Gauss-Seidel Iterative Method",
                new string('=', 60),
                "Formula:  x\u1d62 = (b\u1d62 \u2212 \u03a3\u2c7c\u2260\u1d62 a\u1d62\u2c7c\u00b7x\u2c7c) / a\u1d62\u1d62",
                "Uses updated values immediately (key property of Gauss-Seidel).",
                $"System size: {n}\u00d7{n}   Tolerance: {tol}",
                new string('=', 60)
            });

            double[] x = new double[n];
            for (int k = 0; k < maxIter; k++)
            {
                double[] xOld = (double[])x.Clone();
                res.Log.Add($"\n--- Iteration {k + 1} ---");
                for (int i = 0; i < n; i++)
                {
                    double sigma = 0;
                    for (int j = 0; j < n; j++)
                        if (j != i) sigma += A[i, j] * x[j];
                    if (Math.Abs(A[i, i]) < 1e-15)
                    { res.ErrorMsg = $"Zero diagonal at row {i + 1} — reorder equations."; res.X = x; return res; }
                    x[i] = (b[i] - sigma) / A[i, i];
                    res.Log.Add($"  x{i + 1} = ({b[i]:F6} \u2212 {sigma:F6}) / {A[i, i]:F6} = {x[i]:F8}");
                }
                double maxErr = 0;
                for (int i = 0; i < n; i++)
                    maxErr = Math.Max(maxErr, Math.Abs(x[i] - xOld[i]));

                string xStr = "[" + string.Join(", ", x.Select(v => v.ToString("F6"))) + "]";
                res.Steps.Add(new GaussStep { Iteration = k + 1, XValues = xStr, MaxError = maxErr });
                res.Log.Add($"  \u2192 x = {xStr}   max|\u0394x| = {maxErr:E3}");

                if (maxErr < tol)
                {
                    res.Log.Add($"\n\u2713 Converged in {k + 1} iterations.");
                    res.Log.Add("Solution:  " + string.Join("   ",
                        x.Select((v, i) => $"x{i + 1} = {v:F8}")));
                    res.X = x; res.Converged = true; return res;
                }
            }
            res.Log.Add($"\n\u26a0 Did not converge in {maxIter} iterations.");
            res.X = x; return res;
        }

        // 6. Lagrange Interpolation
        public static InterpResult Lagrange(double[] xs, double[] ys, double xVal)
        {
            int n = xs.Length;
            var res = new InterpResult();
            res.Log.AddRange(new[]
            {
                "Lagrange Interpolation",
                new string('=', 55),
                "P(x) = \u03a3\u1d62 y\u1d62 \u00b7 L\u1d62(x)",
                $"n = {n} data points   evaluate at x = {xVal}",
                new string('=', 55)
            });
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                double Li = 1.0;
                var parts = new List<string>();
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    Li *= (xVal - xs[j]) / (xs[i] - xs[j]);
                    parts.Add($"(x\u2212{xs[j]}) / ({xs[i]}\u2212{xs[j]})");
                }
                double term = ys[i] * Li;
                result += term;
                res.Log.Add($"  L{i}({xVal}) = {string.Join(" \u00b7 ", parts)}");
                res.Log.Add($"           = {Li:F8}");
                res.Log.Add($"  y{i}\u00b7L{i} = {ys[i]} \u00d7 {Li:F6} = {term:F8}");
                res.Log.Add("");
            }
            res.Log.Add($"\u2713 P({xVal}) = {result:F10}");
            res.Value = result;
            return res;
        }

        // 7. Newton Divided Differences
        public static InterpResult NewtonDD(double[] xs, double[] ys)
        {
            int n = xs.Length;
            double[,] dd = new double[n, n];
            for (int i = 0; i < n; i++) dd[i, 0] = ys[i];
            for (int j = 1; j < n; j++)
                for (int i = 0; i < n - j; i++)
                    dd[i, j] = (dd[i + 1, j - 1] - dd[i, j - 1]) / (xs[i + j] - xs[i]);

            var res = new InterpResult();
            res.Log.AddRange(new[]
            {
                "Newton Divided Differences",
                new string('=', 55),
                "Builds divided difference table; evaluates using nested form.",
                new string('=', 55), "",
                "Divided Difference Table:",
                new string('-', 55)
            });
            for (int i = 0; i < n; i++)
            {
                var row = $"  x={xs[i],-8}  " +
                          string.Join("  ", Enumerable.Range(0, n - i)
                              .Select(j => dd[i, j].ToString("F6").PadLeft(12)));
                res.Log.Add(row);
            }
            double[] coeffs = Enumerable.Range(0, n).Select(j => dd[0, j]).ToArray();
            res.Log.Add("");
            res.Log.Add("Coefficients (f[x\u2080], f[x\u2080,x\u2081], ...):");
            res.Log.Add("  [" + string.Join(", ", coeffs.Select(c => c.ToString("F8"))) + "]");
            res.Coeffs = coeffs;
            return res;
        }

        public static double EvalNewtonPoly(double[] coeffs, double[] xs, double xVal)
        {
            double result = coeffs[0];
            for (int i = 1; i < coeffs.Length; i++)
            {
                double term = coeffs[i];
                for (int j = 0; j < i; j++) term *= (xVal - xs[j]);
                result += term;
            }
            return result;
        }

        // 8. Trapezoidal
        public static IntegResult Trapezoidal(MathFunc f, double a, double b, int n)
        {
            double h = (b - a) / n;
            var xs = Enumerable.Range(0, n + 1).Select(i => a + i * h).ToArray();
            var ys = xs.Select(xi => f(xi)).ToArray();
            double s = (ys[0] + ys[n]) / 2.0;
            for (int i = 1; i < n; i++) s += ys[i];
            double result = s * h;

            var res = new IntegResult { Value = result, Xs = xs, Ys = ys };
            res.Log.AddRange(new[]
            {
                "Trapezoidal Rule",
                new string('=', 55),
                "Formula:  h/2 \u00b7 [f(a) + 2\u00b7\u03a3f(x\u1d62) + f(b)]",
                $"[{a}, {b}]   n={n}   h={h:F8}",
                new string('=', 55),
                $"  f(a) = {ys[0]:F10}",
                $"  f(b) = {ys[n]:F10}",
                $"  Interior sum = {ys[1..n].Sum():F10}",
                "",
                $"\u2713 Integral \u2248 {result:F10}"
            });
            return res;
        }

        // 9. Simpson's 1/3
        public static IntegResult Simpsons(MathFunc f, double a, double b, int n)
        {
            if (n % 2 != 0) n++;
            double h = (b - a) / n;
            var xs = Enumerable.Range(0, n + 1).Select(i => a + i * h).ToArray();
            var ys = xs.Select(xi => f(xi)).ToArray();
            double s = ys[0] + ys[n];
            double wSum = 0;
            for (int i = 1; i < n; i++)
            {
                double w = i % 2 != 0 ? 4.0 : 2.0;
                s += w * ys[i]; wSum += w * ys[i];
            }
            double result = s * h / 3.0;

            var res = new IntegResult { Value = result, Xs = xs, Ys = ys };
            res.Log.AddRange(new[]
            {
                "Simpson's 1/3 Rule",
                new string('=', 55),
                "Formula:  h/3 \u00b7 [f(a) + 4f(x\u2081) + 2f(x\u2082) + \u2026 + 4f(x\u2099\u208b\u2081) + f(b)]",
                $"[{a}, {b}]   n={n} (even)   h={h:F8}",
                new string('=', 55),
                $"  f(a) = {ys[0]:F10}",
                $"  f(b) = {ys[n]:F10}",
                $"  Weighted interior sum = {wSum:F10}",
                "",
                $"\u2713 Integral \u2248 {result:F10}"
            });
            return res;
        }

        // Method Suggestion
        public static (string Best, List<string> All, List<string> Reasons)
            SuggestMethod(bool hasDeriv, bool hasInterval, bool signChange, bool hasX0)
        {
            var s = new List<string>(); var r = new List<string>();
            if (hasDeriv && hasX0)
            {
                s.Add("Newton-Raphson");
                r.Add("f'(x) available \u2192 Newton-Raphson gives quadratic convergence.");
            }
            if (hasInterval && signChange)
            {
                s.Add("Bisection");
                r.Add("Sign change in [a,b] \u2192 Bisection is guaranteed to converge.");
            }
            if (hasX0)
            {
                s.Add("Secant");
                r.Add("x\u2080 available, no derivative \u2192 Secant is a strong fallback.");
            }
            if (s.Count == 0) { s.Add("Bisection"); r.Add("No clear info \u2014 Bisection is the safest default."); }
            return (s[0], s, r);
        }
    }
}
