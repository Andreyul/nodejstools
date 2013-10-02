﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.IO;
using Microsoft.NodejsTools;
using Microsoft.NodejsTools.Repl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using TestUtilities.Mocks;

namespace NodejsTests {
    [TestClass]
    public class ReplWindowTests {
        [ClassInitialize]
        public static void DoDeployment(TestContext context) {
            AssertListener.Initialize();
        }

        [TestMethod, Priority(0)]
        public void TestNumber() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("42");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual(window.Output, "42");
            }
        }

        private static NodejsReplEvaluator ProjectlessEvaluator() {
            return new NodejsReplEvaluator(TestNodejsReplSite.Instance);
        }

        [TestMethod, Priority(0)]
        public void TestRequire() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("require('http').constructor");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("[Function: Object]", window.Output);
            }
        }

        [TestMethod, Priority(0)]
        public void TestFunctionDefinition() {
            var whitespaces = new[] { "", "\r\n", "   ", "\r\n    " };
            using (var eval = ProjectlessEvaluator()) {
                foreach (var whitespace in whitespaces) {
                    Console.WriteLine("Whitespace: {0}", whitespace);
                    var window = new MockReplWindow(eval);
                    var res = eval.ExecuteText(whitespace + "function f() { }");
                    Assert.IsTrue(res.Wait(10000));
                    Assert.AreEqual("undefined", window.Output);
                    window.ClearScreen();

                    res = eval.ExecuteText("f");
                    Assert.IsTrue(res.Wait(10000));
                    Assert.AreEqual("[Function: f]", window.Output);
                }
            }
        }

        [TestMethod, Priority(0)]
        public void TestConsoleLog() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("console.log('hi')");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("hi\r\nundefined", window.Output);
            }
        }

        [TestMethod, Priority(0)]
        public void TestConsoleWarn() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("console.warn('hi')");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("hi\r\n", window.Error);
            }
        }

        [TestMethod, Priority(0)]
        public void TestConsoleError() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("console.error('hi')");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("hi\r\n", window.Error);
            }
        }

        [TestMethod, Priority(0)]
        public void TestConsoleDir() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("console.dir({'abc': {'foo': [1,2,3,4,5,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40]}})");
                var expected = @"{ abc: 
   { foo: 
      [ 1,
        2,
        3,
        4,
        5,
        7,
        8,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        32,
        33,
        34,
        35,
        36,
        37,
        38,
        39,
        40 ] } }
undefined";
                Assert.IsTrue(res.Wait(10000));
                var received = window.Output;
                AreEqual(expected, received);
            }
        }

        private static void AreEqual(string expected, string received) {
            for (int i = 0; i < expected.Length && i < received.Length; i++) {
                Assert.AreEqual(expected[i], received[i], String.Format("Mismatch at {0}: expected {1} got {2} in <{3}>", i, expected[i], received[i], received));
            }
            Assert.AreEqual(expected.Length, received.Length, "strings differ by length");
        }

        // 
        [TestMethod, Priority(0)]
        public void LargeOutput() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("var x = 'abc'; for(i = 0; i<12; i++) { x += x; }; x");
                string expected = "abc";
                for (int i = 0; i < 12; i++) {
                    expected += expected;
                }

                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("'" + expected + "'", window.Output);
            }
        }

        [TestMethod, Priority(0)]
        public void TestException() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("throw 'an error';");

                Assert.IsTrue(res.Wait(10000));

                Assert.AreEqual("an error", window.Error);
            }
        }

        [TestMethod, Priority(0)]
        public void TestProcessExit() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("process.exit(0);");

                Assert.IsTrue(res.Wait(10000));

                Assert.AreEqual("The process has exited", window.Error);
                window.ClearScreen();

                res = eval.ExecuteText("42");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("Current interactive window is disconnected - please reset the process.\r\n", window.Error);
            }
        }

        [TestMethod, Priority(0)]
        public void TestReset() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);

                var res = eval.ExecuteText("1");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("1", window.Output);
                res = window.Reset();
                Assert.IsTrue(res.Wait(10000));

                Assert.AreEqual("The process has exited", window.Error);
                window.ClearScreen();
                Assert.AreEqual("", window.Output);
                Assert.AreEqual("", window.Error);

                //Check to ensure the REPL continues to work after Reset
                res = eval.ExecuteText("var a = 1");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("undefined", window.Output);
                res = eval.ExecuteText("a");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("undefined1", window.Output);                
            }
        }

        [TestMethod, Priority(0)]
        public void TestSaveNoFile() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("function f() { }");

                Assert.IsTrue(res.Wait(10000));

                res = eval.ExecuteText("function g() { }");
                Assert.IsTrue(res.Wait(10000));

                new SaveReplCommand().Execute(window, "").Wait(10000);

                Assert.IsTrue(window.Error.Contains("save requires a filename"));
            }
        }

        [TestMethod, Priority(0)]
        public void TestSaveBadFile() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("function f() { }");

                Assert.IsTrue(res.Wait(10000));

                res = eval.ExecuteText("function g() { }");
                Assert.IsTrue(res.Wait(10000));

                new SaveReplCommand().Execute(window, "<foo>").Wait(10000);

                Assert.IsTrue(window.Error.Contains("Invalid filename: <foo>"));
            }
        }

        [TestMethod, Priority(0)]
        public void TestSave() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval, NodejsConstants.JavaScript);
                var res = window.Execute("function f() { }");

                Assert.IsTrue(res.Wait(10000));

                res = window.Execute("function g() { }");
                Assert.IsTrue(res.Wait(10000));

                var path = Path.GetTempFileName();
                File.Delete(path);
                new SaveReplCommand().Execute(window, path).Wait(10000);

                Assert.IsTrue(File.Exists(path));
                var saved = File.ReadAllText(path);

                Assert.IsTrue(saved.IndexOf("function f") != -1);
                Assert.IsTrue(saved.IndexOf("function g") != -1);

                Assert.IsTrue(window.Output.Contains("Session saved to:"));
            }
        }

        [TestMethod, Priority(0)]
        public void TestBadSave() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("function f() { }");

                Assert.IsTrue(res.Wait(10000));

                res = eval.ExecuteText("function g() { }");
                Assert.IsTrue(res.Wait(10000));

                new SaveReplCommand().Execute(window, "C:\\Some\\Directory\\That\\Does\\Not\\Exist\\foo.js").Wait(10000);

                Assert.IsTrue(window.Error.Contains("Failed to save: "));
            }
        }

        [TestMethod, Priority(0)]
        public void ReplEvaluatorProvider() {
            var provider = new NodejsReplEvaluatorProvider();
            Assert.AreEqual(null, provider.GetEvaluator("Unknown"));
            Assert.AreNotEqual(null, provider.GetEvaluator("{E4AC36B7-EDC5-4AD2-B758-B5416D520705}"));
        }

        private static string[] _partialInputs = {  "function f(",
                                                    "function f() {",
                                                    "x = {foo:",
                                                    "{\r\nfoo:42",
                                                    "function () {",
                                                    "for(var i = 0; i<10; i++) {",
                                                    "for(var i = 0; i<10; i++) {\r\nconsole.log('hi');",
                                                    "while(true) {",
                                                    "while(true) {\r\nbreak;",
                                                    "do {",
                                                    "do {\r\nbreak;",
                                                    "if(true) {",
                                                    "if(true) {\r\nconsole.log('hi');",
                                                    "if(true) {\r\nconsole.log('hi');\r\n}else{",
                                                    "if(true) {\r\nconsole.log('hi');\r\n}else{\r\nconsole.log('bye');",
                                                    "switch(\"abc\") {",
                                                    "switch(\"abc\") {\r\ncase \"foo\":",
                                                    "switch(\"abc\") {\r\ncase \"foo\":\r\nbreak;",
                                                    "switch(\"abc\") {\r\ncase \"foo\":\r\nbreak;\r\ncase \"abc\":",
                                                    "switch(\"abc\") {\r\ncase \"foo\":\r\nbreak;\r\ncase \"abc\":console.log('hi');",
                                                    "switch(\"abc\") {\r\ncase \"foo\":\r\nbreak;\r\ncase \"abc\":console.log('hi');\r\nbreak;",
                                                    "[1,",
                                                    "[1,\r\n2,",
                                                    "var net = require('net'),"
                                                   };
        private static string[] _completeInputs = { "try {\r\nconsole.log('hi')\r\n} catch {\r\n}",
                                                    "try {\r\nconsole.log('hi')\r\n} catch(a) {\r\n}",
                                                    "function f(\r\na) {\r\n}\r\n\r\n};",
                                                    "x = {foo}",
                                                    "x = {foo:42}",
                                                    "{x:42}",
                                                    "{\r\nfoo:42\r\n}",
                                                    "function () {\r\nconsole.log('hi');\r\n}",
                                                    "for(var i = 0; i<10; i++) {\r\nconsole.log('hi');\r\n}",
                                                    "while(true) {\r\nbreak;\r\n}",
                                                    "do {\r\nbreak;\r\n}while(true);",
                                                    "if(true) {\r\nconsole.log('hi');\r\n}",
                                                    "if(true) {\r\nconsole.log('hi');\r\n}else{\r\nconsole.log('bye');\r\n}",
                                                    "switch('abc') {\r\ncase 'foo':\r\nbreak;\r\ncase 'abc':\r\nconsole.log('hi');\r\nbreak;\r\n}",
                                                    "[1,\r\n2,\r\n3]",
                                                    "var net = require('net'),\r\n      repl = require('repl');",
                                                  };

        [TestMethod, Priority(0)]
        public void TestPartialInputs() {
            using (var eval = ProjectlessEvaluator()) {
                foreach (var partialInput in _partialInputs) {
                    Assert.AreEqual(false, eval.CanExecuteText(partialInput), "Partial input successfully parsed: " + partialInput);
                }
                foreach (var completeInput in _completeInputs) {
                    Assert.AreEqual(true, eval.CanExecuteText(completeInput), "Complete input failed to parse: " + completeInput);
                }
            }
        }

        [TestMethod, Priority(0)]
        public void TestVarI() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);

                var res = eval.ExecuteText("i");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("ReferenceError: i is not defined", window.Error);
                Assert.AreEqual("", window.Output);
                res = eval.ExecuteText("var i = 987654;");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("undefined", window.Output);
                res = eval.ExecuteText("i");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("undefined987654", window.Output);
            }
        }

        [TestMethod, Priority(0)]
        public void TestObjectLiteral() {
            using (var eval = ProjectlessEvaluator()) {
                var window = new MockReplWindow(eval);
                var res = eval.ExecuteText("{x:42}");
                Assert.IsTrue(res.Wait(10000));
                Assert.AreEqual("{ x: 42 }", window.Output);
            }
        }

        /// <summary>
        /// https://nodejstools.codeplex.com/workitem/279
        /// </summary>
        [TestMethod, Priority(0)]
        public void TestRequireInProject() {
            string testDir;
            do {
                testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(testDir));
            Directory.CreateDirectory(testDir);
            var moduleDir = Path.Combine(testDir, "node_modules");
            Directory.CreateDirectory(moduleDir);
            File.WriteAllText(Path.Combine(moduleDir, "foo.js"), "exports.foo = function(a, b, c) { }");
            File.WriteAllText(Path.Combine(testDir, "bar.js"), "exports.bar = function(a, b, c) { }");

            try {
                using (var eval = new NodejsReplEvaluator(new TestNodejsReplSite(null, testDir))) {
                    var window = new MockReplWindow(eval);
                    var res = eval.ExecuteText("require('foo.js');");
                    Assert.IsTrue(res.Wait(10000));
                    Assert.AreEqual(window.Output, "{ foo: [Function] }");
                    window.ClearScreen();

                    res = eval.ExecuteText("require('./bar.js');");
                    Assert.IsTrue(res.Wait(10000));
                    Assert.AreEqual(window.Output, "{ bar: [Function] }");
                }
            } finally {
                try {
                    Directory.Delete(testDir, true);
                } catch (IOException) {
                }
            }
        }

    }
}