﻿using ExpressionTreeVisualizer;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using static ExpressionTreeTransform.Util.Globals;

namespace _visualizerTests {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            //var i = 7;
            //var j = 8;
            //Expression<Func<bool>> expr = () => i * j <= 25 || new DateTime(1, 1, 1981).Year >= j && new { DateTime.Now }.Now.Day > 10;

            //Expression<Func<int, int>> expr = x => Enumerable.Range(1, x).Select(y => x * y).Count();

            //Expression expr = Lambda(Constant(new DateTime(1980, 1, 1)));

            //Expression<Func<Foo>> expr = () => new Foo("baz") { Bar = "bar" };

            //Expression<Func<List<string>>> expr = () => new List<string> { "abcd", "defg" };

            //Expression<Func<Wrapper>> expr = () => new Wrapper {
            //    {",","2"},
            //    "1",
            //    {"3","4"}
            //};

            //Expression<Func<int, string, bool>> expr = (i, s) => (i * i * i + 15) >= 10 && s.Length <= 25 || (Math.Pow(j, 3) > 100 && j + 15 < 100);

            //var i = 5;
            //Expression<Func<int, int>> expr = j => (i + j + 17) * (i + j + 17);

            //Expression<Func<bool>> expr = () => true;

            //Expression<Func<string, int, string>> expr = (s, i) => $"{s}, {i}";

            //Expression<Func<object[]>> expr = () => new object[] { "" };

            //Expression<Func<string[][]>> expr = () => new string[5][];

            var Bar = "abcd";
            var Baz = "efgh";
            Expression<Func<object>> expr = () => new { Bar, Baz };

            var visualizerHost = new VisualizerDevelopmentHost(expr, typeof(Visualizer),typeof(VisualizerDataObjectSource));
            visualizerHost.ShowVisualizer();

            //Console.ReadKey(true);
        }
    }

    class Foo {
        public string Bar { get; set; }
        public Foo(string baz) { }
    }

    class Wrapper : List<string> {
        public void Add(string s1, string s2) => throw new NotImplementedException();
        public void Add(string s1, string s2, string s3) => throw new NotImplementedException();
    }
}
