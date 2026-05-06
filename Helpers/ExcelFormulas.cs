using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualBasic;
using System.Runtime.Intrinsics.X86;

namespace SciSolve.Helpers
{
    public static class ExcelFormulas
    {
        public static readonly Dictionary<string, string> All = new()
        {
            ["Newton-Raphson"] =
@"Newton-Raphson Method — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   x_new = x − f(x) / f'(x)

LAYOUT:
  Column A │ Iteration number  (1, 2, 3, …)
  Column B │ x value
  Column C │ f(x)              e.g.  =B2^3 - B2 - 2
  Column D │ f'(x)             e.g.  =3*B2^2 - 1
  Column E │ x_new             =B2 - C2/D2
  Column F │ |error|           =ABS(E2 - B2)
  Column G │ Status            =IF(F2<1E-6,""✓  Converged"",""…"")

SETUP STEPS:
  1.Enter your initial guess x₀ in cell B2.
  2.Enter your f(x) formula in C2(use B2 for x).
  3.Enter your f'(x) formula in D2 (use B2 for x).
  4.E2,
            F2,
            G2 are automatic from the formulas above.
  5.In B3 type:  = E2(carry x_new forward)
  6.Drag row 3 down as far as needed.
  7.Stop at the first ✓ in column G.

TIPS:
  • Guard zero derivative:  = IF(D2 = 0, ""ERR!"", B2 - C2 / D2)
  • Use conditional formatting to highlight converged rows green.
  • Works best when f(x) is smooth and x₀ is close to the root.
",

            ["Secant"] =
@"Secant Method — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   x₂ = x₁ − f(x₁)·(x₁−x₀) / (f(x₁)−f(x₀))
No derivative required — uses two previous points.

LAYOUT:
  Column A │ Iteration
  Column B │ x₀  (previous point)
  Column C │ x₁  (current point)
  Column D │ f(x₀)   e.g.  =B2^3 - B2 - 2
  Column E │ f(x₁)   e.g.  =C2^3 - C2 - 2
  Column F │ x₂      =C2 - E2*(C2-B2)/(E2-D2)
  Column G │ |error| =ABS(F2-C2)
  Column H │ Status  =IF(G2<1E-6,""✓"",""…"")

SETUP STEPS:
  1.Enter x₀ in B2,
            x₁ in C2.
  2.Enter your f(x) formula in D2(using B2) and E2(using C2).
  3.  F2, G2, H2 are automatic.
  4.  Next row:  B3=C2,  C3=F2
  5.  Drag down until column H shows ✓.

GUARD:  =IF(E2-D2= 0,""div/0!"", C2 - E2*(C2-B2)/(E2-D2))
",

            ["Bisection"] =
@"Bisection Method — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   c = (a+b)/2   →  narrow the bracket each step
REQUIREMENT: f(a)·f(b) < 0  (sign change must exist)

LAYOUT:
  Column A │ Iteration
  Column B │ a  (left bound)
  Column C │ b  (right bound)
  Column D │ c = (a+b)/2         =(B2+C2)/2
  Column E │ f(c)                =D2^3 - D2 - 2
  Column F │ f(a)                =B2^3 - B2 - 2
  Column G │ Interval width      =ABS(C2-B2)/2
  Column H │ Status              =IF(G2<1E-6,""✓"",""…"")

NEXT ROW FORMULAS(drag down) :
  B3 = =IF(F2* E2<0, B2, D2)   ← keep left  half if sign change
  C3 = =IF(F2* E2<0, D2, C2)   ← keep right half otherwise

VERIFY BEFORE STARTING:
  =IF((B2^3-B2-2)* (C2^3-C2-2)<0,""✓ Bracket OK"",""⚠ Same sign!"")
",

            ["Fixed Point"] =
@"Fixed Point Iteration — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   x_new = g(x)   until  |x_new − x| < tolerance
REQUIREMENT: |g'(x)| < 1 near the root for convergence.

LAYOUT:
  Column A │ Iteration
  Column B │ xₙ
  Column C │ g(xₙ)   e.g.  =(B2+2)^(1/3)
  Column D │ |error| =ABS(C2-B2)
  Column E │ Status  =IF(D2<1E-6,""✓  Converged"",""…"")

SETUP STEPS:
  1.  Enter initial x₀ in B2.
  2.  Enter your g(x) formula in C2(use B2 for x).
  3.  D2, E2 are automatic.
  4.  In B3 type:  =C2(carry g(x) forward as new x)
  5.  Drag down until column E shows ✓.

NOTE:
  If the iteration diverges, your g(x) does not satisfy
  | g'(x)| < 1.  Rewrite f(x)=0 as x=g(x) differently,
  or switch to Newton-Raphson.
",

            ["Gauss-Seidel"] =
@"Gauss-Seidel Method — Excel Setup  (3×3 example)
══════════════════════════════════════════════════════════

SYSTEM:
  10x₁ −  x₂ + 2x₃ =   6
  − x₁ + 11x₂ −  x₃ =  25
    2x₁ −  x₂ + 10x₃ = −11

COEFFICIENT PLACEMENT:
  Rows 2–4, Columns B–D = matrix A;  Column E = vector b

  B2=10  C2=-1   D2=2    E2=6
  B3=-1  C3=11   D3=-1   E3=25
  B4=2   C4=-1   D4=10   E4=-11

INITIAL GUESS (row 6):  G6=0,  H6=0,  I6=0

ITERATION FORMULAS (row 7 onwards):
  G7 = ($E$2 - $C$2*H6 - $D$2*I6) / $B$2      ← uses OLD H6, I6
  H7 = ($E$3 - $B$3*G7 - $D$3*I6) / $C$3      ← uses UPDATED G7
  I7 = ($E$4 - $B$4*G7 - $C$4*H7) / $D$4      ← uses UPDATED G7, H7

  J7 = MAX(ABS(G7-G6), ABS(H7-H6), ABS(I7-I6))   ← max|Δx|
  K7 = IF(J7<1E-6,""✓  Converged"",""…"")

Drag rows 7 + down until column K shows ✓.

KEY RULE: Gauss-Seidel uses each updated value immediately
within the same iteration(unlike Jacobi which waits).
",

            ["Trapezoidal Rule"] =
@"Trapezoidal Rule — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   ∫f(x)dx ≈ h/2 · [f(a) + 2·Σf(xᵢ) + f(b)]
where  h = (b−a)/n

PARAMETER CELLS:
  B1 = a (lower bound)
  B2 = b (upper bound)
  B3 = n (number of subintervals)
  B4 = h = =(B2-B1)/B3

DATA TABLE (rows 7 onward):
  Column A: i        A7=0, A8=1, … drag down n+1 rows
  Column B: xᵢ       B7=$B$1, B8=B7+$B$4  (drag down)
  Column C: f(xᵢ)    C7=SIN(B7)  ← replace SIN with your f(x)
  Column D: weight   D7=1 (first), D8=2 (interior), last=1
  Column E: contrib  E7=C7*D7  (drag down)

RESULT:
  =($B$4/2) * (C7 + 2*SUM(C8:C<last-1>) + C<last>)
  Or:  =($B$4/2) * SUMPRODUCT(C7:C<last>, D7:D<last>)

ERROR ESTIMATE:
  |Error| ≤ (b−a)³ / (12n²) · max|f''(x)|
  Double n to reduce error by factor of 4.
",

            ["Simpson's 1/3 Rule"] =
@"Simpson's 1/3 Rule — Excel Setup
══════════════════════════════════════════════════════════

FORMULA:   h/3 · [f(a) + 4f(x₁) + 2f(x₂) + 4f(x₃) + … + f(b)]
REQUIREMENT:  n MUST be even.

PARAMETER CELLS:
  B1 = a,   B2 = b
  B3 = n    ← MUST BE EVEN!
  B4 = h = =(B2-B1)/B3
  B5 = =IF(MOD(B3,2)=0,""n is even ✓"",""⚠ n must be even!"")

DATA TABLE (rows 8 onward):
  Column A: i        A8=0, A9=1, … drag n+1 rows
  Column B: xᵢ       B8=$B$1, B9=B8+$B$4
  Column C: f(xᵢ)    C8=SIN(B8) ← replace with your f(x)
  Column D: weight   D8=1,  D9=4,  D10=2,  D11=4, …  last=1
             =IF(OR(A8=0,A8=$B$3), 1, IF(MOD(A8,2)=1, 4, 2))
  Column E: contrib  E8=C8*D8

RESULT:
  =($B$4/3) * SUMPRODUCT(C8:C<last>, D8:D<last>)

ERROR ESTIMATE:
  |Error| ≤ (b−a)⁵ / (180n⁴) · max|f⁴(x)|
  Simpson's is ~100× more accurate than Trapezoidal at same n.
",

            ["Lagrange"] =
@"Lagrange Interpolation — Excel Setup  (4 data points)
══════════════════════════════════════════════════════════

FORMULA:   P(x) = Σᵢ yᵢ · Lᵢ(x)
where  Lᵢ(x) = Πⱼ≠ᵢ (x−xⱼ) / (xᵢ−xⱼ)

DATA:
  B2:B5 = x values  (x₀, x₁, x₂, x₃)
  C2:C5 = y values  (f(x₀), f(x₁), f(x₂), f(x₃))
  E1    = target x  (the value where you want P(x))

BASIS POLYNOMIALS:
  L0 = PRODUCT((E1-B3),(E1-B4),(E1-B5)) /
       PRODUCT((B2-B3),(B2-B4),(B2-B5))

  L1 = PRODUCT((E1-B2),(E1-B4),(E1-B5)) /
       PRODUCT((B3-B2),(B3-B4),(B3-B5))

  L2 = PRODUCT((E1-B2),(E1-B3),(E1-B5)) /
       PRODUCT((B4-B2),(B4-B3),(B4-B5))

  L3 = PRODUCT((E1-B2),(E1-B3),(E1-B4)) /
       PRODUCT((B5-B2),(B5-B3),(B5-B4))

RESULT:
  P(E1) = =C2*L0 + C3*L1 + C4*L2 + C5*L3

NOTES:
  • Does NOT require equally-spaced points.
  • For n points use n basis polynomials — extend the pattern.
  • Watch for Runge's phenomenon with many points at equal spacing.
  • For large n, use Newton Divided Differences instead.
"
        };
    }
}
