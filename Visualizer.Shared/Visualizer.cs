﻿using Microsoft.VisualStudio.DebuggerVisualizers;
using System;
using System.Diagnostics;
using System.Windows;
using static ExpressionToString.FormatterNames;

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer), 
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource), 
    Target = typeof(System.Linq.Expressions.Expression), 
    Description ="Expression Tree Visualizer")]

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer),
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource),
    Target = typeof(System.Linq.Expressions.ElementInit),
    Description = "Expression Tree Visualizer")]

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer),
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource),
    Target = typeof(System.Linq.Expressions.MemberBinding),
    Description = "Expression Tree Visualizer")]

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer),
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource),
    Target = typeof(System.Linq.Expressions.SwitchCase),
    Description = "Expression Tree Visualizer")]

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer),
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource),
    Target = typeof(System.Linq.Expressions.CatchBlock),
    Description = "Expression Tree Visualizer")]

[assembly: DebuggerVisualizer(
    visualizer: typeof(ExpressionTreeVisualizer.Visualizer),
    visualizerObjectSource: typeof(ExpressionTreeVisualizer.VisualizerDataObjectSource),
    Target = typeof(System.Linq.Expressions.LabelTarget),
    Description = "Expression Tree Visualizer")]

namespace ExpressionTreeVisualizer {
    public class Visualizer : DialogDebuggerVisualizer {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider) {
            if (windowService == null) { throw new ArgumentNullException(nameof(windowService)); }

            FrameworkCompatibilityPreferences.AreInactiveSelectionHighlightBrushKeysSupported = false;

            var window = new VisualizerWindow();
            var control = window.Content as VisualizerDataControl;
            control.ObjectProvider = objectProvider;
            control.Options = new VisualizerDataOptions() { Formatter = CSharp }; // TODO options could come from a VS extension

            window.ShowDialog();
        }
    }
}
