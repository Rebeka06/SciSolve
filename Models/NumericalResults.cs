namespace SciSolve.Models
{
    public class IterationStep
    {
        public int Iteration { get; set; }
        public double X { get; set; }
        public string FX { get; set; } = "";
        public double Error { get; set; }
        public string XFormatted => X.ToString("F10");
        public string ErrorFormatted => Error.ToString("E4");
    }

    public class GaussStep
    {
        public int Iteration { get; set; }
        public string XValues { get; set; } = "";
        public double MaxError { get; set; }
        public string MaxErrorFormatted => MaxError.ToString("E4");
    }

    public class SolveResult
    {
        public double Root { get; set; }
        public bool Converged { get; set; }
        public string? ErrorMsg { get; set; }
        public List<double> Errors { get; set; } = new();
        public List<IterationStep> Steps { get; set; } = new();
        public List<string> Log { get; set; } = new();
    }

    public class GaussResult
    {
        public double[] X { get; set; } = Array.Empty<double>();
        public bool Converged { get; set; }
        public string? ErrorMsg { get; set; }
        public List<GaussStep> Steps { get; set; } = new();
        public List<string> Log { get; set; } = new();
    }

    public class InterpResult
    {
        public double Value { get; set; }
        public double[]? Coeffs { get; set; }
        public List<string> Log { get; set; } = new();
    }

    public class IntegResult
    {
        public double Value { get; set; }
        public double[] Xs { get; set; } = Array.Empty<double>();
        public double[] Ys { get; set; } = Array.Empty<double>();
        public List<string> Log { get; set; } = new();
    }

    public class IntegTableRow
    {
        public int I { get; set; }
        public double Xi { get; set; }
        public double FXi { get; set; }
        public double Weight { get; set; }
        public double Contribution { get; set; }
        public string XiFormatted => Xi.ToString("F8");
        public string FXiFormatted => FXi.ToString("F8");
        public string ContribFormatted => Contribution.ToString("F8");
    }

    public class LagrangeRow
    {
        public int I { get; set; }
        public double Xi { get; set; }
        public double Yi { get; set; }
        public double Li { get; set; }
        public double YiLi { get; set; }
        public string LiFormatted => Li.ToString("F10");
        public string YiLiFormatted => YiLi.ToString("F10");
    }
}
