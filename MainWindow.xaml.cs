using Microsoft.Win32;
using SciSolve.Helpers;
using SciSolve.Models;
using SciSolve_WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SciSolve
{
    public partial class MainWindow : Window
    {
        // ── Palette (mirrors App.xaml) ────────────────────────────────────────
        private static readonly SolidColorBrush BrWhite = Brush("#D0DCE8");
        private static readonly SolidColorBrush BrMuted = Brush("#8BA0B4");
        private static readonly SolidColorBrush BrNavy = Brush("#0D1B2A");
        private static readonly SolidColorBrush BrNavyMid = Brush("#1B2A3B");
        private static readonly SolidColorBrush BrNavyCard = Brush("#1E2D3D");
        private static readonly SolidColorBrush BrGray = Brush("#2A3D50");
        private static readonly SolidColorBrush BrGreen = Brush("#2ECC71");
        private static readonly SolidColorBrush BrGreenDark = Brush("#27AE60");
        private static SolidColorBrush Brush(string hex) =>
            new(ColorFromHex(hex));
        private static Color ColorFromHex(string hex) =>
            (Color)ColorConverter.ConvertFromString(hex);

        // ── Session stores for export ─────────────────────────────────────────
        private SolveResult? _lastEqResult;
        private string _lastEqMethod = "";
        private string _lastEqFuncExpr = "";
        private GaussResult? _lastGsResult;
        private double[,]? _lastGsA;
        private double[]? _lastGsB;
        private InterpResult? _lastInterpResult;
        private double[]? _lastInterpXs, _lastInterpYs;
        private double _lastInterpXVal;
        private string _lastInterpMethod = "";
        private IntegResult? _lastIntegResult;
        private string _lastIntegMethod = "";
        private string _lastIntegFuncExpr = "";
        private double _lastIntegA, _lastIntegB;
        private int _lastIntegN;
        private MathFunc? _lastIntegF;

        // ── Gauss matrix UI ───────────────────────────────────────────────────
        private TextBox[,]? _gsCellsA;
        private TextBox[]? _gsCellsB;
        private int _gsN = 3;

        // ── Observable collections bound to DataGrids ────────────────────────
        public ObservableCollection<IterationStep> EqSteps { get; } = new();
        public ObservableCollection<GaussStep> GsSteps { get; } = new();
        public ObservableCollection<LagrangeRow> InterpRows { get; } = new();
        public ObservableCollection<IntegTableRow> IntegRows { get; } = new();

        // =====================================================================
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            DgEqSteps.ItemsSource = EqSteps;
            DgGsSteps.ItemsSource = GsSteps;
            DgInterpRows.ItemsSource = InterpRows;
            DgIntRows.ItemsSource = IntegRows;

            BuildGaussMatrix(3);
            LbExcelMethods.SelectedIndex = 0;
            ShowExcelFormula("Newton-Raphson");
        }

        // =====================================================================
        // ── HELPER: parse a double from a TextBox ────────────────────────────
        private static double D(TextBox tb, double fallback = 0) =>
            double.TryParse(tb.Text.Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double v) ? v : fallback;

        private static int I(TextBox tb, int fallback = 100) =>
            int.TryParse(tb.Text.Trim(), out int v) ? v : fallback;

        private static double? Opt(TextBox tb)
        {
            var s = tb.Text.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            return double.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double v) ? v : (double?)null;
        }

        private void Err(string msg) =>
            MessageBox.Show(msg, "SciSolve Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);

        // =====================================================================
        // TAB 1 — EQUATION SOLVING
        // =====================================================================
        private async void BtnSolveEquation_Click(object s, RoutedEventArgs e)
        {
            var f = FunctionParser.Parse(TbFx.Text.Trim());
            var df = FunctionParser.Parse(TbDfx.Text.Trim());
            var g = FunctionParser.Parse(TbGx.Text.Trim());

            if (f == null) { Err("Cannot parse f(x). Check your expression."); return; }

            double tol = D(TbEqTol, 1e-6);
            double x0 = D(TbX0, 1.5);
            double x1 = D(TbX1, 2.0);
            double? a = Opt(TbA);
            double? b = Opt(TbB);
            int mi = I(TbEqMaxIter, 100);

            string methodSel = (CbEqMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Auto";
            string method = methodSel;
            string note = "";

            if (methodSel.StartsWith("Auto"))
            {
                bool sc = a.HasValue && b.HasValue && (f(a.Value) * f(b.Value) < 0);
                var (best, _, reasons) = NumericalMethods.SuggestMethod(
                    df != null, a.HasValue && b.HasValue, sc, true);
                method = best;
                note = $"Auto-selected: {best}\n" +
                         string.Join("\n", reasons.Select(r => $"  • {r}")) + "\n\n";
            }

            _lastEqMethod = method;
            _lastEqFuncExpr = TbFx.Text.Trim();

            SolveResult? res = null;
            try
            {
                res = await Task.Run(() =>
                {
                    return method switch
                    {
                        "Newton-Raphson" => df == null
                            ? throw new InvalidOperationException("Newton-Raphson requires f'(x).")
                            : NumericalMethods.Newton(f, df, x0, tol, mi),
                        "Secant" => NumericalMethods.Secant(f, x0, x1, tol, mi),
                        "Bisection" => (!a.HasValue || !b.HasValue)
                            ? throw new InvalidOperationException("Bisection requires [a, b].")
                            : NumericalMethods.Bisection(f, a.Value, b.Value, tol, mi),
                        "Fixed Point" => g == null
                            ? throw new InvalidOperationException("Fixed Point requires g(x).")
                            : NumericalMethods.FixedPoint(g, x0, tol, mi),
                        _ => throw new InvalidOperationException($"Unknown method: {method}")
                    };
                });
            }
            catch (Exception ex) { Err(ex.Message); return; }

            if (res.ErrorMsg != null) { Err(res.ErrorMsg); return; }

            _lastEqResult = res;

            // Update log
            TbEqLog.Text = note + string.Join("\n", res.Log);
            TbEqLog.ScrollToEnd();

            // Update table
            EqSteps.Clear();
            foreach (var step in res.Steps) EqSteps.Add(step);

            // Result banner
            EqResultBorder.Visibility = Visibility.Visible;
            EqResultText.Text = res.Converged
                ? $"✓  Root ≈ {res.Root:F10}   ({res.Steps.Count} iterations)   Method: {method}"
                : $"⚠  Did not converge in {mi} iterations.  Best estimate: {res.Root:F10}";
            EqResultText.Foreground = res.Converged ? BrGreen : BrMuted;

            // Chart
            DrawConvergenceChart(EqChartCanvas, res.Errors, "Convergence  |error|");
        }

        private void BtnExportEquation_Click(object s, RoutedEventArgs e)
        {
            if (_lastEqResult == null) { Err("Run a calculation first."); return; }
            var dlg = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = $"SciSolve_{_lastEqMethod}.xlsx",
                Title = "Save Equation Export"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ExcelExporter.ExportEquation(dlg.FileName, _lastEqMethod,
                    _lastEqResult.Steps, _lastEqResult.Root,
                    _lastEqResult.Converged, _lastEqFuncExpr);
                MessageBox.Show($"Saved!\n\n{System.IO.Path.GetFileName(dlg.FileName)}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Err($"Export failed: {ex.Message}"); }
        }

        // =====================================================================
        // TAB 2 — GAUSS-SEIDEL
        // =====================================================================
        private void CbGsSize_Changed(object s, SelectionChangedEventArgs e)
        {
            int n = CbGsSize.SelectedIndex + 2;
            BuildGaussMatrix(n);
        }

        private void BtnGsRebuild_Click(object s, RoutedEventArgs e)
        {
            int n = CbGsSize.SelectedIndex + 2;
            BuildGaussMatrix(n);
        }

        private void BuildGaussMatrix(int n)
        {
            _gsN = n;
            GsMatrixGrid.Children.Clear();
            GsMatrixGrid.RowDefinitions.Clear();
            GsMatrixGrid.ColumnDefinitions.Clear();

            // Columns: n for A + 1 separator + 1 for b
            for (int c = 0; c <= n + 1; c++)
                GsMatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(c == n ? 16 : 1, c == n ? GridUnitType.Pixel : GridUnitType.Star) });
            for (int r = 0; r < n; r++)
                GsMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });

            _gsCellsA = new TextBox[n, n];
            _gsCellsB = new TextBox[n];

            double[,] defA = { { 10, -1, 2, 0 }, { -1, 11, -1, 0 }, { 2, -1, 10, 0 }, { 0, 0, 0, 5 } };
            double[] defB = { 6, 25, -11, 0 };

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var tb = MakeMatrixCell(i < 4 && j < 4 ? defA[i, j].ToString() : (i == j ? "5" : "0"));
                    Grid.SetRow(tb, i); Grid.SetColumn(tb, j);
                    GsMatrixGrid.Children.Add(tb);
                    _gsCellsA[i, j] = tb;
                }
                // Separator
                var sep = new TextBlock
                {
                    Text = "│",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = BrMuted,
                    FontSize = 18,
                };
                Grid.SetRow(sep, i); Grid.SetColumn(sep, n);
                GsMatrixGrid.Children.Add(sep);

                // b column
                var tb2 = MakeMatrixCell(i < 4 ? defB[i].ToString() : "0");
                Grid.SetRow(tb2, i); Grid.SetColumn(tb2, n + 1);
                GsMatrixGrid.Children.Add(tb2);
                _gsCellsB[i] = tb2;
            }
        }

        private static TextBox MakeMatrixCell(string val) => new()
        {
            Text = val,
            Width = 52,
            Height = 28,
            Background = new SolidColorBrush(ColorFromHex("#0D1B2A")),
            Foreground = new SolidColorBrush(ColorFromHex("#F5A623")),
            BorderBrush = new SolidColorBrush(ColorFromHex("#2E4057")),
            BorderThickness = new Thickness(1),
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
        };

        private async void BtnSolveGauss_Click(object s, RoutedEventArgs e)
        {
            if (_gsCellsA == null || _gsCellsB == null) { Err("Matrix not initialised."); return; }
            double[,] A = new double[_gsN, _gsN];
            double[] b = new double[_gsN];
            try
            {
                for (int i = 0; i < _gsN; i++)
                {
                    for (int j = 0; j < _gsN; j++)
                        A[i, j] = double.Parse(_gsCellsA[i, j].Text,
                            System.Globalization.CultureInfo.InvariantCulture);
                    b[i] = double.Parse(_gsCellsB[i].Text,
                        System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch { Err("Invalid matrix values — ensure all cells contain numbers."); return; }

            double tol = D(TbGsTol, 1e-6);
            int mi = I(TbGsMaxIter, 100);

            GaussResult? res = null;
            try { res = await Task.Run(() => NumericalMethods.GaussSeidel(A, b, tol, mi)); }
            catch (Exception ex) { Err(ex.Message); return; }

            if (res.ErrorMsg != null) { Err(res.ErrorMsg); return; }

            _lastGsResult = res; _lastGsA = A; _lastGsB = b;

            TbGsLog.Text = string.Join("\n", res.Log);
            TbGsLog.ScrollToEnd();

            GsSteps.Clear();
            foreach (var st in res.Steps) GsSteps.Add(st);

            GsResultBorder.Visibility = Visibility.Visible;
            GsResultText.Text = res.Converged
                ? "✓  " + string.Join("   ", res.X.Select((v, i) => $"x{i + 1} = {v:F8}"))
                : $"⚠  Did not converge in {mi} iterations.";
            GsResultText.Foreground = res.Converged ? BrGreen : BrMuted;

            DrawConvergenceChart(GsChartCanvas,
                res.Steps.Select(st => st.MaxError).ToList(), "max|Δx|");
        }

        private void BtnExportGauss_Click(object s, RoutedEventArgs e)
        {
            if (_lastGsResult == null || _lastGsA == null || _lastGsB == null)
            { Err("Run Gauss-Seidel first."); return; }
            var dlg = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "SciSolve_GaussSeidel.xlsx",
                Title = "Save Gauss-Seidel Export"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ExcelExporter.ExportGaussSeidel(dlg.FileName,
                    _lastGsResult.Steps, _lastGsResult.X, _gsN, _lastGsA, _lastGsB);
                MessageBox.Show($"Saved!\n\n{System.IO.Path.GetFileName(dlg.FileName)}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Err($"Export failed: {ex.Message}"); }
        }

        // =====================================================================
        // TAB 3 — INTERPOLATION
        // =====================================================================
        private void BtnInterpolate_Click(object s, RoutedEventArgs e)
        {
            double[] xs, ys;
            try
            {
                xs = TbInterpX.Text.Split(',').Select(v =>
                    double.Parse(v.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                ys = TbInterpY.Text.Split(',').Select(v =>
                    double.Parse(v.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            }
            catch { Err("Invalid x or y values. Use comma-separated numbers."); return; }

            if (xs.Length != ys.Length)
            { Err("x and y must have the same number of values."); return; }
            if (xs.Length < 2) { Err("Need at least 2 data points."); return; }

            double xVal = D(TbInterpXVal, 2.5);
            string method = (CbInterpMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lagrange";

            InterpResult res;
            double result;
            if (method == "Lagrange")
            {
                res = NumericalMethods.Lagrange(xs, ys, xVal);
                result = res.Value;
            }
            else
            {
                res = NumericalMethods.NewtonDD(xs, ys);
                result = NumericalMethods.EvalNewtonPoly(res.Coeffs!, xs, xVal);
                res.Log.Add($"\n✓ P({xVal}) = {result:F10}");
                res.Value = result;
            }

            _lastInterpResult = res; _lastInterpXs = xs; _lastInterpYs = ys;
            _lastInterpXVal = xVal; _lastInterpMethod = method;

            TbInterpLog.Text = string.Join("\n", res.Log);
            TbInterpLog.ScrollToEnd();

            // Build Lagrange rows for table
            InterpRows.Clear();
            for (int i = 0; i < xs.Length; i++)
            {
                double Li = 1.0;
                for (int j = 0; j < xs.Length; j++)
                    if (j != i) Li *= (xVal - xs[j]) / (xs[i] - xs[j]);
                InterpRows.Add(new LagrangeRow
                {
                    I = i,
                    Xi = xs[i],
                    Yi = ys[i],
                    Li = Li,
                    YiLi = ys[i] * Li
                });
            }

            InterpResultBorder.Visibility = Visibility.Visible;
            InterpResultText.Text = $"✓  P({xVal}) = {result:F10}   Method: {method}";
            InterpResultText.Foreground = BrGreen;

            DrawInterpolationChart(InterpChartCanvas, xs, ys, xVal, result,
                xv => method == "Lagrange"
                    ? NumericalMethods.Lagrange(xs, ys, xv).Value
                    : NumericalMethods.EvalNewtonPoly(res.Coeffs!, xs, xv));
        }

        private void BtnExportInterp_Click(object s, RoutedEventArgs e)
        {
            if (_lastInterpResult == null)
            { Err("Run interpolation first."); return; }
            var dlg = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "SciSolve_Interpolation.xlsx",
                Title = "Save Interpolation Export"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ExcelExporter.ExportInterpolation(dlg.FileName, _lastInterpMethod,
                    _lastInterpXs!, _lastInterpYs!, _lastInterpXVal,
                    _lastInterpResult.Value, InterpRows.ToList());
                MessageBox.Show($"Saved!\n\n{System.IO.Path.GetFileName(dlg.FileName)}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Err($"Export failed: {ex.Message}"); }
        }

        // =====================================================================
        // TAB 4 — INTEGRATION
        // =====================================================================
        private void BtnIntegrate_Click(object s, RoutedEventArgs e)
        {
            var f = FunctionParser.Parse(TbIntF.Text.Trim());
            if (f == null) { Err("Cannot parse f(x)."); return; }

            double a = D(TbIntA, 0);
            double b = D(TbIntB, Math.PI);
            int n = I(TbIntN, 100);
            string method = (CbIntMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Trapezoidal Rule";

            IntegResult res;
            if (method.StartsWith("Trap"))
                res = NumericalMethods.Trapezoidal(f, a, b, n);
            else
                res = NumericalMethods.Simpsons(f, a, b, n);

            _lastIntegResult = res;
            _lastIntegMethod = method;
            _lastIntegFuncExpr = TbIntF.Text.Trim();
            _lastIntegA = a; _lastIntegB = b; _lastIntegN = n;
            _lastIntegF = f;

            TbIntLog.Text = string.Join("\n", res.Log);
            TbIntLog.ScrollToEnd();

            // Build table (show up to 25 rows)
            IntegRows.Clear();
            bool isTrap = method.StartsWith("Trap");
            int dispN = Math.Min(n, 25);
            for (int i = 0; i <= dispN; i++)
            {
                double xi = res.Xs[i];
                double yi = res.Ys[i];
                double w = (i == 0 || i == n) ? 1.0
                            : isTrap ? 2.0
                            : (i % 2 != 0 ? 4.0 : 2.0);
                IntegRows.Add(new IntegTableRow
                {
                    I = i,
                    Xi = xi,
                    FXi = yi,
                    Weight = w,
                    Contribution = yi * w
                });
            }

            IntResultBorder.Visibility = Visibility.Visible;
            IntResultText.Text = $"✓  ∫f(x)dx ≈ {res.Value:F10}   [{a} → {b}]   n={n}   Method: {method}";
            IntResultText.Foreground = BrGreen;

            DrawIntegrationChart(IntChartCanvas, f, a, b, method, res.Value, n);
        }

        private void BtnExportInteg_Click(object s, RoutedEventArgs e)
        {
            if (_lastIntegResult == null) { Err("Run integration first."); return; }
            var dlg = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "SciSolve_Integration.xlsx",
                Title = "Save Integration Export"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ExcelExporter.ExportIntegration(dlg.FileName, _lastIntegMethod,
                    _lastIntegA, _lastIntegB, _lastIntegN,
                    _lastIntegResult.Value, _lastIntegFuncExpr,
                    IntegRows.ToList());
                MessageBox.Show($"Saved!\n\n{System.IO.Path.GetFileName(dlg.FileName)}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Err($"Export failed: {ex.Message}"); }
        }

        // =====================================================================
        // TAB 5 — METHOD ADVISOR
        // =====================================================================
        private void BtnAdvise_Click(object s, RoutedEventArgs e)
        {
            var (best, all, reasons) = NumericalMethods.SuggestMethod(
                ChkHasDeriv.IsChecked == true,
                ChkHasInterval.IsChecked == true,
                ChkSignChange.IsChecked == true,
                ChkHasX0.IsChecked == true);

            var advice = new Dictionary<string, string>
            {
                ["Newton-Raphson"] =
                    "Best for: smooth functions where f'(x) is easy to compute.\n" +
                    "Pros:  Quadratic convergence — error squares each iteration.\n" +
                    "Cons:  Fails if f'(x) = 0 near root. Requires derivative.\n" +
                    "Order: Quadratic.",
                ["Bisection"] =
                    "Best for: any continuous f on [a,b] with a sign change.\n" +
                    "Pros:  Always converges. Very robust. No derivative needed.\n" +
                    "Cons:  Slow linear convergence. Requires bracket [a,b].\n" +
                    "Order: Linear (error halves each step).",
                ["Secant"] =
                    "Best for: when derivative is unavailable or expensive.\n" +
                    "Pros:  Superlinear convergence. No derivative needed.\n" +
                    "Cons:  May diverge. Needs two starting points x₀, x₁.\n" +
                    "Order: Superlinear (≈ 1.618, the golden ratio).",
                ["Fixed Point"] =
                    "Best for: equations rewritten as x = g(x).\n" +
                    "Pros:  Very simple to implement.\n" +
                    "Cons:  Convergence only when |g'(x)| < 1 near root.\n" +
                    "Order: Linear.",
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"✓  PRIMARY RECOMMENDATION:  {best}");
            sb.AppendLine(new string('═', 56));
            sb.AppendLine(advice.GetValueOrDefault(best, ""));
            sb.AppendLine();
            sb.AppendLine(new string('═', 56));
            sb.AppendLine("REASONING:");
            foreach (var r in reasons) sb.AppendLine($"  •  {r}");

            if (all.Count > 1)
            {
                sb.AppendLine();
                sb.AppendLine(new string('═', 56));
                sb.AppendLine("OTHER APPLICABLE METHODS:");
                foreach (var m in all.Skip(1))
                {
                    sb.AppendLine();
                    sb.AppendLine($"  ◆  {m}:");
                    foreach (var line in (advice.GetValueOrDefault(m, "")).Split('\n'))
                        sb.AppendLine("     " + line);
                }
            }

            sb.AppendLine();
            sb.AppendLine(new string('═', 56));
            sb.AppendLine("QUICK COMPARISON TABLE:");
            sb.AppendLine($"{"Method",-22} {"Order",-16} {"Needs df/dx",-14} {"Needs bracket",-14}");
            sb.AppendLine(new string('-', 56));
            sb.AppendLine($"{"Newton-Raphson",-22} {"Quadratic",-16} {"Yes",-14} {"No",-14}");
            sb.AppendLine($"{"Secant",-22} {"≈ 1.618",-16} {"No",-14} {"No",-14}");
            sb.AppendLine($"{"Bisection",-22} {"Linear",-16} {"No",-14} {"Yes",-14}");
            sb.AppendLine($"{"Fixed Point",-22} {"Linear",-16} {"No",-14} {"No",-14}");

            TbAdviceLog.Text = sb.ToString();
            TbAdviceLog.ScrollToHome();
        }

        // =====================================================================
        // TAB 6 — EXCEL FORMULAS
        // =====================================================================
        private void LbExcelMethods_Changed(object s, SelectionChangedEventArgs e)
        {
            var item = LbExcelMethods.SelectedItem as ListBoxItem;
            if (item == null) return;
            ShowExcelFormula(item.Content?.ToString() ?? "");
        }

        private void ShowExcelFormula(string method)
        {
            if (ExcelFormulas.All.TryGetValue(method, out var text))
            {
                TbExcelFormula.Text = text;
                TbExcelMethodTitle.Text = $"{method} — Formula Guide";
            }
        }

        // =====================================================================
        // CHART DRAWING  (pure WPF Canvas — no external library)
        // =====================================================================

        // ── Convergence line chart (log scale on Y) ───────────────────────────
        private void DrawConvergenceChart(Canvas cv, List<double> errors, string yLabel)
        {
            cv.Children.Clear();
            if (errors.Count < 2) return;

            double w = cv.ActualWidth > 0 ? cv.ActualWidth : 600;
            double h = cv.ActualHeight > 0 ? cv.ActualHeight : 160;
            double pad = 50;

            double maxE = errors.Max();
            double minE = errors.Min();
            if (minE <= 0) minE = 1e-16;
            double logMax = Math.Log10(maxE) + 0.5;
            double logMin = Math.Log10(minE) - 0.5;
            double logRange = logMax - logMin;
            if (logRange <= 0) logRange = 1;

            double plotW = w - pad * 2;
            double plotH = h - pad * 1.5;

            Func<int, double> px = i => pad + i * plotW / (errors.Count - 1);
            Func<double, double> py = v => {
                double lv = Math.Log10(Math.Max(v, 1e-16));
                return (pad * 0.8) + (logMax - lv) / logRange * plotH;
            };

            // Grid lines
            for (int decade = (int)Math.Floor(logMin); decade <= (int)Math.Ceiling(logMax); decade++)
            {
                double yy = py(Math.Pow(10, decade));
                if (yy < 0 || yy > h) continue;
                cv.Children.Add(new Line
                {
                    X1 = pad,
                    Y1 = yy,
                    X2 = w - pad / 2,
                    Y2 = yy,
                    Stroke = new SolidColorBrush(ColorFromHex("#2A3D50")),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                });
                cv.Children.Add(new TextBlock
                {
                    Text = $"1e{decade}",
                    Foreground = BrMuted,
                    FontSize = 9,
                    FontFamily = new FontFamily("Consolas"),
                });
                Canvas.SetLeft(cv.Children[^1] as UIElement ?? new UIElement(), 2);
                Canvas.SetTop(cv.Children[^1] as UIElement ?? new UIElement(), yy - 8);
            }

            // Axes
            cv.Children.Add(new Line
            {
                X1 = pad,
                Y1 = pad * 0.8 - 4,
                X2 = pad,
                Y2 = h - pad * 0.5 + 4,
                Stroke = BrMuted,
                StrokeThickness = 1
            });
            cv.Children.Add(new Line
            {
                X1 = pad - 4,
                Y1 = h - pad * 0.5,
                X2 = w - pad / 2,
                Y2 = h - pad * 0.5,
                Stroke = BrMuted,
                StrokeThickness = 1
            });

            // X-axis labels
            int step = Math.Max(1, errors.Count / 8);
            for (int i = 0; i < errors.Count; i += step)
            {
                double xx = px(i);
                var tb = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    Foreground = BrMuted,
                    FontSize = 9,
                };
                Canvas.SetLeft(tb, xx - 6);
                Canvas.SetTop(tb, h - pad * 0.5 + 2);
                cv.Children.Add(tb);
            }

            // Y label
            var yLbl = new TextBlock
            {
                Text = yLabel,
                Foreground = BrMuted,
                FontSize = 9,
                RenderTransform = new RotateTransform(-90),
            };
            Canvas.SetLeft(yLbl, 2); Canvas.SetTop(yLbl, h / 2 + 20);
            cv.Children.Add(yLbl);

            // Data line
            var pts = new PointCollection();
            for (int i = 0; i < errors.Count; i++)
                pts.Add(new Point(px(i), py(errors[i])));

            cv.Children.Add(new Polyline
            {
                Points = pts,
                Stroke = BrGreen,
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            });

            // Dots
            foreach (var pt in pts)
            {
                var d = new Ellipse { Width = 7, Height = 7, Fill = BrGreenDark, Stroke = BrGreen, StrokeThickness = 1.5 };
                Canvas.SetLeft(d, pt.X - 3.5); Canvas.SetTop(d, pt.Y - 3.5);
                cv.Children.Add(d);
            }
        }

        // ── Interpolation curve ───────────────────────────────────────────────
        private void DrawInterpolationChart(Canvas cv,
                                             double[] xs, double[] ys,
                                             double xVal, double result,
                                             Func<double, double> poly)
        {
            cv.Children.Clear();
            double w = cv.ActualWidth > 0 ? cv.ActualWidth : 600;
            double h = cv.ActualHeight > 0 ? cv.ActualHeight : 160;
            double pad = 46;

            double xMin = xs.Min() - 0.5, xMax = xs.Max() + 0.5;
            int pN = 200;
            var curve = Enumerable.Range(0, pN + 1)
                           .Select(i => xMin + i * (xMax - xMin) / pN)
                           .Select(xi => { try { return (xi, poly(xi)); } catch { return (xi, double.NaN); } })
                           .Where(p => !double.IsNaN(p.Item2))
                           .ToList();

            if (curve.Count < 2) return;

            double yMin = Math.Min(ys.Min(), curve.Min(p => p.Item2));
            double yMax = Math.Max(ys.Max(), curve.Max(p => p.Item2));
            double yPad = (yMax - yMin) * 0.12;
            yMin -= yPad; yMax += yPad;
            if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 1; yMax += 1; }

            double plotW = w - pad * 2, plotH = h - pad * 1.8;
            Func<double, double> px = x => pad + (x - xMin) / (xMax - xMin) * plotW;
            Func<double, double> py = y => (pad * 0.8) + (yMax - y) / (yMax - yMin) * plotH;

            // Grid lines
            DrawAxisGrid(cv, pad, w, h, plotH, yMin, yMax, xMin, xMax, px, py);

            // Polynomial curve
            var curvePts = new PointCollection(curve.Select(p => new Point(px(p.xi), py(p.Item2))));
            cv.Children.Add(new Polyline
            {
                Points = curvePts,
                Stroke = BrWhite,
                StrokeThickness = 1.8,
            });

            // Data points
            foreach (var (xi, yi) in xs.Zip(ys))
            {
                var dot = new Ellipse { Width = 9, Height = 9, Fill = BrNavy, Stroke = BrGreen, StrokeThickness = 2 };
                Canvas.SetLeft(dot, px(xi) - 4.5); Canvas.SetTop(dot, py(yi) - 4.5);
                cv.Children.Add(dot);
            }

            // Evaluated point
            var evalDot = new Ellipse { Width = 12, Height = 12, Fill = BrGreen, Stroke = BrGreenDark, StrokeThickness = 2 };
            Canvas.SetLeft(evalDot, px(xVal) - 6); Canvas.SetTop(evalDot, py(result) - 6);
            cv.Children.Add(evalDot);
        }

        // ── Integration area chart ─────────────────────────────────────────────
        private void DrawIntegrationChart(Canvas cv, MathFunc f,
                                           double a, double b,
                                           string method, double result, int n)
        {
            cv.Children.Clear();
            double w = cv.ActualWidth > 0 ? cv.ActualWidth : 600;
            double h = cv.ActualHeight > 0 ? cv.ActualHeight : 160;
            double pad = 46;

            int pN = 300;
            double step = (b - a) / pN;
            var xyAll = Enumerable.Range(0, pN + 1)
                           .Select(i => (x: a + i * step, y: f(a + i * step)))
                           .ToList();

            double yMin = Math.Min(0, xyAll.Min(p => p.y));
            double yMax = Math.Max(0, xyAll.Max(p => p.y));
            double yPad = (yMax - yMin) * 0.12;
            yMin -= yPad; yMax += yPad;
            if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 1; yMax += 1; }

            double plotW = w - pad * 2, plotH = h - pad * 1.8;
            Func<double, double> px = x => pad + (x - a) / (b - a) * plotW;
            Func<double, double> py = yv => (pad * 0.8) + (yMax - yv) / (yMax - yMin) * plotH;

            DrawAxisGrid(cv, pad, w, h, plotH, yMin, yMax, a, b, px, py);

            // Shaded panels (up to 40)
            int dispN = Math.Min(n, 40);
            double hd = (b - a) / dispN;
            var shadeColor = new SolidColorBrush(Color.FromArgb(80, 46, 204, 113));
            for (int i = 0; i < dispN; i++)
            {
                double x0 = a + i * hd, x1 = a + (i + 1) * hd;
                double y0 = f(x0), y1 = f(x1);
                double zero = py(0);
                var poly = new Polygon
                {
                    Fill = shadeColor,
                    Stroke = new SolidColorBrush(Color.FromArgb(120, 46, 204, 113)),
                    StrokeThickness = 0.5,
                };
                poly.Points.Add(new Point(px(x0), zero));
                poly.Points.Add(new Point(px(x0), py(y0)));
                poly.Points.Add(new Point(px(x1), py(y1)));
                poly.Points.Add(new Point(px(x1), zero));
                cv.Children.Add(poly);
            }

            // Zero line
            if (yMin < 0 && yMax > 0)
                cv.Children.Add(new Line
                {
                    X1 = pad,
                    Y1 = py(0),
                    X2 = w - pad / 2,
                    Y2 = py(0),
                    Stroke = BrMuted,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                });

            // Curve
            var pts = new PointCollection(xyAll.Select(p => new Point(px(p.x), py(p.y))));
            cv.Children.Add(new Polyline { Points = pts, Stroke = BrWhite, StrokeThickness = 2 });

            // Title
            var title = new TextBlock
            {
                Text = $"{method}  ≈  {result:F8}  (n={n})",
                Foreground = BrGreen,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
            };
            Canvas.SetLeft(title, pad + 4); Canvas.SetTop(title, 2);
            cv.Children.Add(title);
        }

        // ── Shared axis + grid helper ──────────────────────────────────────────
        private static void DrawAxisGrid(Canvas cv,
                                          double pad, double w, double h, double plotH,
                                          double yMin, double yMax,
                                          double xMin, double xMax,
                                          Func<double, double> px, Func<double, double> py)
        {
            var gridColor = new SolidColorBrush(ColorFromHex("#2A3D50"));

            // Horizontal grid
            int hLines = 5;
            for (int i = 0; i <= hLines; i++)
            {
                double yv = yMin + i * (yMax - yMin) / hLines;
                double yy = py(yv);
                cv.Children.Add(new Line
                {
                    X1 = pad,
                    Y1 = yy,
                    X2 = w - pad / 2,
                    Y2 = yy,
                    Stroke = gridColor,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                });
                var axTb = new TextBlock
                {
                    Text = yv.ToString("F3"),
                    Foreground = new SolidColorBrush(ColorFromHex("#8BA0B4")),
                    FontSize = 8.5,
                    FontFamily = new FontFamily("Consolas"),
                };
                Canvas.SetLeft(axTb, 1.0); Canvas.SetTop(axTb, yy - 7);
                cv.Children.Add(axTb);
            }

            // Axes
            cv.Children.Add(new Line
            {
                X1 = pad,
                Y1 = pad * 0.8 - 4,
                X2 = pad,
                Y2 = h - pad * 0.5 + 4,
                Stroke = new SolidColorBrush(ColorFromHex("#8BA0B4")),
                StrokeThickness = 1
            });
            cv.Children.Add(new Line
            {
                X1 = pad - 4,
                Y1 = h - pad * 0.5,
                X2 = w - pad / 2,
                Y2 = h - pad * 0.5,
                Stroke = new SolidColorBrush(ColorFromHex("#8BA0B4")),
                StrokeThickness = 1
            });
        }

        // ── Layout updated → redraw charts ────────────────────────────────────
        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            base.OnRenderSizeChanged(info);
            RedrawAllCharts();
        }

        private void RedrawAllCharts()
        {
            if (_lastEqResult?.Errors.Count > 1)
                DrawConvergenceChart(EqChartCanvas, _lastEqResult.Errors, "Convergence |error|");
            if (_lastGsResult?.Steps.Count > 1)
                DrawConvergenceChart(GsChartCanvas,
                    _lastGsResult.Steps.Select(s => s.MaxError).ToList(), "max|Δx|");
            if (_lastIntegResult != null && _lastIntegF != null)
                DrawIntegrationChart(IntChartCanvas, _lastIntegF,
                    _lastIntegA, _lastIntegB, _lastIntegMethod,
                    _lastIntegResult.Value, _lastIntegN);
        }
    }
}
