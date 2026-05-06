using ClosedXML.Excel;
using SciSolve.Models;
using System.IO;

namespace SciSolve.Helpers
{
    public static class ExcelExporter
    {
        // colour palette
        private static readonly XLColor NavyDark = XLColor.FromHtml("#0D1B2A");
        private static readonly XLColor NavyMid = XLColor.FromHtml("#1B2A3B");
        private static readonly XLColor NavyCard = XLColor.FromHtml("#1E2D3D");
        private static readonly XLColor NavyBorder = XLColor.FromHtml("#2E4057");
        private static readonly XLColor White = XLColor.FromHtml("#D0DCE8");
        private static readonly XLColor Muted = XLColor.FromHtml("#8BA0B4");
        private static readonly XLColor Green = XLColor.FromHtml("#2ECC71");
        private static readonly XLColor GreenDark = XLColor.FromHtml("#1A3A28");
        private static readonly XLColor Amber = XLColor.FromHtml("#F5A623");
        private static readonly XLColor GrayBorder = XLColor.FromHtml("#2A3D50");

 
        // EQUATION SOLVING EXPORT
        public static void ExportEquation(string filePath, string method,
                                           List<IterationStep> steps, double root,
                                           bool converged, string funcExpr)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet(SanitiseName($"SciSolve_{method}"));
            ws.ShowGridLines = false;

            // Title
            MergeTitle(ws, 1, 1, 8,
                $"SciSolve  —  {method}",
                NavyDark, White, 16, true);
            MergeTitle(ws, 2, 1, 8,
                $"f(x) = {funcExpr}   |   Result: {(converged ? $"Root ≈ {root:F10}" : "Did not converge")}",
                NavyMid, Muted, 11, false);
            SetRowH(ws, 1, 30); SetRowH(ws, 2, 16);

            // Column widths
            SetColW(ws, 1, 8); SetColW(ws, 2, 22); SetColW(ws, 3, 22);
            SetColW(ws, 4, 18); SetColW(ws, 5, 16);

            // Header row
            int r = 4;
            string[] hdrs = { "Iter", "x value", "f(x)", "|error|", "Status" };
            for (int c = 1; c <= hdrs.Length; c++)
            {
                var cell = ws.Cell(r, c);
                cell.Value = hdrs[c - 1];
                StyleHeader(cell);
            }
            SetRowH(ws, r, 20); r++;

            // Data rows
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                bool conv = s.Error < 1e-6 && converged && i == steps.Count - 1;
                var bg = i % 2 == 0 ? NavyDark : NavyCard;

                StyleData(ws.Cell(r, 1), s.Iteration.ToString(), bg);
                StyleData(ws.Cell(r, 2), s.X.ToString("F10"), bg);
                StyleData(ws.Cell(r, 3), s.FX, bg);
                StyleData(ws.Cell(r, 4), s.Error.ToString("E4"), bg);
                var statusCell = ws.Cell(r, 5);
                statusCell.Value = conv ? "✓  Converged" : "…";
                statusCell.Style.Fill.BackgroundColor = conv ? GreenDark : bg;
                statusCell.Style.Font.FontColor = conv ? Green : Muted;
                statusCell.Style.Font.FontName = "Consolas";
                statusCell.Style.Font.FontSize = 10;
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Conditional: green row on convergence
                if (conv)
                    for (int c = 1; c <= 5; c++)
                        ws.Cell(r, c).Style.Fill.BackgroundColor = GreenDark;

                SetRowH(ws, r, 18); r++;
            }

            // Result summary row
            SetRowH(ws, r, 24);
            var resRange = ws.Range(r, 1, r, 3);
            resRange.Merge();
            resRange.FirstCell().Value = "RESULT:";
            resRange.FirstCell().Style.Fill.BackgroundColor = NavyMid;
            resRange.FirstCell().Style.Font.FontColor = Amber;
            resRange.FirstCell().Style.Font.Bold = true;
            resRange.FirstCell().Style.Font.FontSize = 12;
            resRange.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var resVal = ws.Range(r, 4, r, 5);
            resVal.Merge();
            resVal.FirstCell().Value = converged ? $"Root ≈ {root:F10}" : "Did not converge";
            resVal.FirstCell().Style.Fill.BackgroundColor = converged ? GreenDark : NavyMid;
            resVal.FirstCell().Style.Font.FontColor = converged ? Green : Muted;
            resVal.FirstCell().Style.Font.Bold = true;
            resVal.FirstCell().Style.Font.FontSize = 13;
            resVal.FirstCell().Style.Font.FontName = "Consolas";
            r++;

            wb.SaveAs(filePath);
        }

        // GAUSS-SEIDEL EXPORT
        public static void ExportGaussSeidel(string filePath,
                                              List<GaussStep> steps,
                                              double[] solution, int n,
                                              double[,] A, double[] b)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("SciSolve_Gauss-Seidel");
            ws.ShowGridLines = false;

            MergeTitle(ws, 1, 1, n + 3, "SciSolve  —  Gauss-Seidel Iterative Method",
                       NavyDark, White, 16, true);
            MergeTitle(ws, 2, 1, n + 3,
                "Formula:  xᵢ = (bᵢ − Σⱼ≠ᵢ aᵢⱼ·xⱼ) / aᵢᵢ   |   Uses updated values immediately",
                NavyMid, Muted, 11, false);
            SetRowH(ws, 1, 30); SetRowH(ws, 2, 16);

            // Matrix A | b
            int mr = 4;
            MergeTitle(ws, mr, 1, n + 1, "  System  Ax = b", NavyMid, White, 11, true);
            mr++;
            for (int c = 1; c <= n; c++) SetColW(ws, c, 12);
            SetColW(ws, n + 1, 4); SetColW(ws, n + 2, 12);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var cell = ws.Cell(mr + i, j + 1);
                    cell.Value = A[i, j];
                    cell.Style.Fill.BackgroundColor = NavyCard;
                    cell.Style.Font.FontColor = Amber;
                    cell.Style.Font.FontName = "Consolas";
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = GrayBorder;
                }
                ws.Cell(mr + i, n + 1).Value = "|";
                ws.Cell(mr + i, n + 1).Style.Font.FontColor = Muted;
                ws.Cell(mr + i, n + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                var bc = ws.Cell(mr + i, n + 2);
                bc.Value = b[i];
                bc.Style.Fill.BackgroundColor = NavyCard;
                bc.Style.Font.FontColor = Amber;
                bc.Style.Font.FontName = "Consolas";
                bc.Style.Font.FontSize = 10;
                bc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                SetRowH(ws, mr + i, 18);
            }
            mr += n + 1;

            // Iteration table
            int r = mr + 1;
            string[] hdrs = new string[] { "Iter" }
                .Concat(Enumerable.Range(1, n).Select(i => $"x{i}"))
                .Concat(new[] { "max|Δx|", "Status" })
                .ToArray();

            for (int c = 1; c <= hdrs.Length; c++)
            {
                SetColW(ws, c, c == 1 ? 8 : c <= n + 1 ? 16 : 14);
                StyleHeader(ws.Cell(r, c));
                ws.Cell(r, c).Value = hdrs[c - 1];
            }
            SetRowH(ws, r, 20); r++;

            bool found = false;
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                var bg = i % 2 == 0 ? NavyDark : NavyCard;
                bool cv = s.MaxError < 1e-6;
                if (cv && !found) found = true;

                StyleData(ws.Cell(r, 1), s.Iteration.ToString(), bg);
                var xVals = s.XValues.Trim('[', ']').Split(',');
                for (int j = 0; j < Math.Min(n, xVals.Length); j++)
                    StyleData(ws.Cell(r, j + 2), xVals[j].Trim(), cv ? GreenDark : bg);
                StyleData(ws.Cell(r, n + 2), s.MaxError.ToString("E4"), bg);
                var sc = ws.Cell(r, n + 3);
                sc.Value = cv ? "✓  YES" : "…";
                sc.Style.Fill.BackgroundColor = cv ? GreenDark : bg;
                sc.Style.Font.FontColor = cv ? Green : Muted;
                sc.Style.Font.FontName = "Consolas";
                sc.Style.Font.FontSize = 10;
                sc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                SetRowH(ws, r, 18); r++;
            }

            // Solution
            SetRowH(ws, r, 24);
            var labRange = ws.Range(r, 1, r, 2);
            labRange.Merge(); labRange.FirstCell().Value = "SOLUTION:";
            labRange.FirstCell().Style.Fill.BackgroundColor = NavyMid;
            labRange.FirstCell().Style.Font.FontColor = Amber;
            labRange.FirstCell().Style.Font.Bold = true;
            labRange.FirstCell().Style.Font.FontSize = 12;
            labRange.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            for (int i = 0; i < n; i++)
            {
                var sc = ws.Cell(r, i + 3);
                sc.Value = $"x{i + 1}={solution[i]:F8}";
                sc.Style.Fill.BackgroundColor = GreenDark;
                sc.Style.Font.FontColor = Green;
                sc.Style.Font.FontName = "Consolas";
                sc.Style.Font.FontSize = 11;
                sc.Style.Font.Bold = true;
            }

            wb.SaveAs(filePath);
        }


        // INTERPOLATION EXPORT
        public static void ExportInterpolation(string filePath, string method,
                                                double[] xs, double[] ys,
                                                double xVal, double result,
                                                List<LagrangeRow> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("SciSolve_Interpolation");
            ws.ShowGridLines = false;

            MergeTitle(ws, 1, 1, 7, $"SciSolve  —  {method}", NavyDark, White, 16, true);
            MergeTitle(ws, 2, 1, 7,
                $"P({xVal}) = {result:F10}   |   {xs.Length} data points",
                NavyMid, Muted, 11, false);
            SetRowH(ws, 1, 30); SetRowH(ws, 2, 16);

            // Data points
            int r = 4;
            MergeTitle(ws, r, 1, 3, "  Data Points", NavyMid, White, 11, true); r++;
            string[] dhdrs = { "i", "xᵢ", "yᵢ = f(xᵢ)" };
            for (int c = 1; c <= 3; c++) { SetColW(ws, c, 14); StyleHeader(ws.Cell(r, c)); ws.Cell(r, c).Value = dhdrs[c - 1]; }
            SetRowH(ws, r, 20); r++;
            for (int i = 0; i < xs.Length; i++)
            {
                var bg = i % 2 == 0 ? NavyDark : NavyCard;
                StyleData(ws.Cell(r, 1), i.ToString(), bg);
                StyleData(ws.Cell(r, 2), xs[i].ToString("F6"), bg);
                StyleData(ws.Cell(r, 3), ys[i].ToString("F6"), bg);
                SetRowH(ws, r, 18); r++;
            }
            r++;

            // Basis polynomial table
            MergeTitle(ws, r, 1, 5, "  Basis Polynomials", NavyMid, White, 11, true); r++;
            string[] bhdrs = { "i", "xᵢ", "yᵢ", "Lᵢ(x_target)", "yᵢ · Lᵢ" };
            for (int c = 1; c <= 5; c++)
            {
                SetColW(ws, c, c >= 4 ? 18 : 14);
                StyleHeader(ws.Cell(r, c));
                ws.Cell(r, c).Value = bhdrs[c - 1];
            }
            SetRowH(ws, r, 20); r++;
            foreach (var row in rows)
            {
                var bg = row.I % 2 == 0 ? NavyDark : NavyCard;
                StyleData(ws.Cell(r, 1), row.I.ToString(), bg);
                StyleData(ws.Cell(r, 2), row.Xi.ToString("F6"), bg);
                StyleData(ws.Cell(r, 3), row.Yi.ToString("F6"), bg);
                StyleData(ws.Cell(r, 4), row.LiFormatted, bg);
                StyleData(ws.Cell(r, 5), row.YiLiFormatted, bg);
                SetRowH(ws, r, 18); r++;
            }

            // Result
            SetRowH(ws, r, 26);
            var lr = ws.Range(r, 1, r, 3); lr.Merge();
            lr.FirstCell().Value = $"P({xVal}) = Σ yᵢ·Lᵢ =";
            lr.FirstCell().Style.Fill.BackgroundColor = NavyMid;
            lr.FirstCell().Style.Font.FontColor = Amber;
            lr.FirstCell().Style.Font.Bold = true;
            lr.FirstCell().Style.Font.FontSize = 12;
            lr.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rr = ws.Range(r, 4, r, 5); rr.Merge();
            rr.FirstCell().Value = result.ToString("F10");
            rr.FirstCell().Style.Fill.BackgroundColor = GreenDark;
            rr.FirstCell().Style.Font.FontColor = Green;
            rr.FirstCell().Style.Font.Bold = true;
            rr.FirstCell().Style.Font.FontSize = 14;
            rr.FirstCell().Style.Font.FontName = "Consolas";
            rr.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            wb.SaveAs(filePath);
        }


        // INTEGRATION EXPORT
        public static void ExportIntegration(string filePath, string method,
                                              double a, double b, int n,
                                              double result, string funcExpr,
                                              List<IntegTableRow> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet($"SciSolve_{(method.Contains("Trap") ? "Trapezoidal" : "Simpsons")}");
            ws.ShowGridLines = false;

            MergeTitle(ws, 1, 1, 6, $"SciSolve  —  {method}", NavyDark, White, 16, true);
            MergeTitle(ws, 2, 1, 6,
                $"∫ f(x)dx from {a} to {b}   n={n}   f(x) = {funcExpr}",
                NavyMid, Muted, 11, false);
            SetRowH(ws, 1, 30); SetRowH(ws, 2, 16);

            // Parameters
            int r = 4;
            MergeTitle(ws, r, 1, 4, "  Parameters", NavyMid, White, 11, true); r++;
            var paramData = new[] {
                ("a (lower bound)", a.ToString("F8")),
                ("b (upper bound)", b.ToString("F8")),
                ("n (subintervals)", n.ToString()),
                ("h = (b−a)/n", ((b-a)/n).ToString("F8"))
            };
            foreach (var (lbl, val) in paramData)
            {
                ws.Cell(r, 1).Value = lbl;
                ws.Cell(r, 1).Style.Fill.BackgroundColor = NavyMid;
                ws.Cell(r, 1).Style.Font.FontColor = Muted;
                ws.Cell(r, 1).Style.Font.FontSize = 11;
                ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(r, 2).Value = val;
                ws.Cell(r, 2).Style.Fill.BackgroundColor = NavyCard;
                ws.Cell(r, 2).Style.Font.FontColor = Amber;
                ws.Cell(r, 2).Style.Font.FontName = "Consolas";
                ws.Cell(r, 2).Style.Font.FontSize = 11;
                SetRowH(ws, r, 18); r++;
            }
            r++;

            // Table
            SetColW(ws, 1, 8); SetColW(ws, 2, 18); SetColW(ws, 3, 16);
            SetColW(ws, 4, 12); SetColW(ws, 5, 18);
            string[] hdrs = { "i", "xᵢ", "f(xᵢ)", "Weight", "Contribution" };
            for (int c = 1; c <= 5; c++) { StyleHeader(ws.Cell(r, c)); ws.Cell(r, c).Value = hdrs[c - 1]; }
            SetRowH(ws, r, 20); r++;

            foreach (var row in rows)
            {
                var bg = row.I % 2 == 0 ? NavyDark : NavyCard;
                StyleData(ws.Cell(r, 1), row.I.ToString(), bg);
                StyleData(ws.Cell(r, 2), row.XiFormatted, bg);
                StyleData(ws.Cell(r, 3), row.FXiFormatted, bg);
                StyleData(ws.Cell(r, 4), row.Weight.ToString(), bg);
                StyleData(ws.Cell(r, 5), row.ContribFormatted, bg);
                SetRowH(ws, r, 18); r++;
            }

            // Result
            SetRowH(ws, r, 26);
            var lr = ws.Range(r, 1, r, 3); lr.Merge();
            lr.FirstCell().Value = "∫ f(x)dx ≈";
            lr.FirstCell().Style.Fill.BackgroundColor = NavyMid;
            lr.FirstCell().Style.Font.FontColor = Amber;
            lr.FirstCell().Style.Font.Bold = true;
            lr.FirstCell().Style.Font.FontSize = 13;
            lr.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rr = ws.Range(r, 4, r, 5); rr.Merge();
            rr.FirstCell().Value = result.ToString("F10");
            rr.FirstCell().Style.Fill.BackgroundColor = GreenDark;
            rr.FirstCell().Style.Font.FontColor = Green;
            rr.FirstCell().Style.Font.Bold = true;
            rr.FirstCell().Style.Font.FontSize = 14;
            rr.FirstCell().Style.Font.FontName = "Consolas";
            rr.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            wb.SaveAs(filePath);
        }


        // HELPER METHODS
        private static void MergeTitle(IXLWorksheet ws, int row, int c1, int c2,
                                        string text, XLColor bg, XLColor fg,
                                        int size, bool bold)
        {
            var range = ws.Range(row, c1, row, c2);
            range.Merge();
            var cell = range.FirstCell();
            cell.Value = text;
            cell.Style.Fill.BackgroundColor = bg;
            cell.Style.Font.FontColor = fg;
            cell.Style.Font.FontSize = size;
            cell.Style.Font.Bold = bold;
            cell.Style.Font.FontName = "Segoe UI";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.Indent = 2;
        }

        private static void StyleHeader(IXLCell cell)
        {
            cell.Style.Fill.BackgroundColor = NavyMid;
            cell.Style.Font.FontColor = White;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontName = "Segoe UI";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = GrayBorder;
        }

        private static void StyleData(IXLCell cell, string value, XLColor bg)
        {
            cell.Value = value;
            cell.Style.Fill.BackgroundColor = bg;
            cell.Style.Font.FontColor = White;
            cell.Style.Font.FontName = "Consolas";
            cell.Style.Font.FontSize = 10;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = GrayBorder;
        }

        private static void SetRowH(IXLWorksheet ws, int row, double h) =>
            ws.Row(row).Height = h;

        private static void SetColW(IXLWorksheet ws, int col, double w) =>
            ws.Column(col).Width = w;

        private static string SanitiseName(string name) =>
            string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'))
                  [..Math.Min(name.Length, 31)];
    }
}
