# SciSolve WPF — Numerical Methods Learning Tool

SciSolve is a WPF-based educational software designed to help students learn and visualize numerical methods step-by-step.  
It provides interactive solving, graphical visualization, and Excel export for mathematical computations.

---

## Project Purpose

The goal of SciSolve is to:
- Simplify learning of numerical methods
- Provide step-by-step solutions
- Visualize mathematical behavior
- Integrate Excel for structured educational reporting

---

## Project Structure

SciSolve_WPF/
│
├── SciSolve.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── FeatureItem.cs
│
├── Models/
│   └── NumericalResults.cs
│
├── Helpers/
│   ├── FunctionParser.cs
│   ├── NumericalMethods.cs
│   ├── ExcelExporter.cs
│   └── ExcelFormulas.cs

---

## Technologies Used

- C# (.NET WPF)
- XAML (UI Design)
- ClosedXML (Excel Export)
- LiveChartsCore (Graph Visualization)

---

## Core Features

### Root Finding Methods
- Newton-Raphson Method  
- Bisection Method  
- Secant Method  
- Fixed Point Iteration  

### Systems of Equations
- Gauss-Seidel Method  

### Interpolation
- Lagrange Interpolation  
- Newton Divided Differences  

### Numerical Integration
- Trapezoidal Rule  
- Simpson’s Rule  

### Graph Visualization
- Function plotting  
- Convergence behavior visualization  
- Error tracking graphs  

### Excel Integration
- Step-by-step export of solutions  
- Structured worksheets per method  
- Automatic formula explanations  
- Report generation using ClosedXML  

---

## Architecture Design

- Models → stores result data structures  
- Helpers → numerical algorithms and logic  
- MainWindow.xaml → UI layout (7-tab system)  
- MainWindow.xaml.cs → event handling and UI logic  

---

## Core Flow

User Input → FunctionParser → NumericalMethods → NumericalResults → UI Display → Excel Export (optional)

---

## UI Design

- Navy Blue theme  
- White background  
- Green accents for Excel and success states  
- Clean academic interface  

---

## Future Improvements

- AI-based method suggestion system  
- Advanced graph interaction  
- PDF report export  
- Dark mode support  
- Performance optimization  

---

## Educational Value

SciSolve combines:
- Mathematics (numerical analysis)
- Programming (C# WPF)
- Data visualization
- Software architecture principles  

It helps students understand step-by-step numerical computation processes.

---

## Author

Student project focused on numerical methods, visualization, and WPF development.