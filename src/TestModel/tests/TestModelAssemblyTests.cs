// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.IO;
using NUnit.Framework;
using TestCentric.Tests.Assemblies;
using NUnit.Engine;

namespace TestCentric.Gui.Model
{
    public class TestModelAssemblyTests
    {
        private const string MOCK_ASSEMBLY = "mock-assembly.dll";

        private ITestModel _model;

        [SetUp]
        public void CreateTestModel()
        {
            var engine = TestEngineActivator.CreateInstance();
            Assert.NotNull(engine, "Unable to create engine instance for testing");

            _model = new TestModel(engine);
            _model.LoadTests(new[] { Path.Combine(TestContext.CurrentContext.TestDirectory, MOCK_ASSEMBLY) });
        }

        [TearDown]
        public void ReleaseTestModel()
        {
            _model.Dispose();
        }

        [Test]
        public void CheckStateAfterLoading()
        {
            Assert.That(_model.HasTests, "HasTests");
            Assert.NotNull(_model.LoadedTests, "Tests");
            Assert.False(_model.HasResults, "HasResults");

            var testRun = _model.LoadedTests;
            Assert.That(testRun.Xml.Name, Is.EqualTo("test-run"), "Expected test-run element");
            Assert.That(testRun.RunState, Is.EqualTo(RunState.Runnable), "RunState of test-run");
            Assert.That(testRun.TestCount, Is.EqualTo(MockAssembly.Tests), "TestCount of test-run");
            Assert.That(testRun.Children.Count, Is.EqualTo(1), "Child count of test-run");

            var testAssembly = testRun.Children[0];
            Assert.That(testAssembly.RunState, Is.EqualTo(RunState.Runnable), "RunState of assembly");
            Assert.That(testAssembly.TestCount, Is.EqualTo(MockAssembly.Tests), "TestCount of assembly");
        }

        [TestCase("MockTestFixture")]
        [TestCase("FailingTest")]
        public void CheckGetTestById(string testName)
        {
            var testNode = FindTestByName(_model.LoadedTests, testName);
            Assert.NotNull(testNode, $"Internal Test Error: Can't find {testName} in mock-assembly");

            var foundTest = _model.GetTestById(testNode.Id);
            Assert.NotNull(foundTest, $"No test found with id {testNode.Id}");
            Assert.That(foundTest.Name, Is.EqualTo(testName), "Found the test but name is wrong");
        }

        private TestNode FindTestByName(TestNode parent, string name)
        {
            if (parent.Name == name)
                return parent;

            foreach (var child in parent.Children)
            {
                var result = FindTestByName(child, name);
                if (result != null) return result;
            }

            return null;
        }

        [Test]
        public void CheckThatTestsMapToPackages()
        {
            var package1 = _model.GetPackageForTest(_model.LoadedTests.Id);
            var package2 = _model.GetPackageForTest(_model.LoadedTests.Children[0].Id);
            var nopackage = _model.GetPackageForTest(_model.LoadedTests.Children[0].Children[0].Id);

            Assert.NotNull(package1, "Package1");
            Assert.NotNull(package2, "Package2");
            Assert.Null(nopackage);

            Assert.Null(package1.Name);
            Assert.That(package1.SubPackages.Count, Is.EqualTo(1));

            Assert.That(package2.Name, Is.EqualTo(MOCK_ASSEMBLY));
            Assert.That(package2.SubPackages.Count, Is.Zero);

            Assert.That(package2, Is.SameAs(package1.SubPackages[0]));
        }

        [Test]
        public void CheckStateAfterRunningTests()
        {
            RunAllTestsAndWaitForCompletion();

            Assert.That(_model.HasTests, "HasTests");
            Assert.NotNull(_model.LoadedTests, "Tests");
            Assert.That(_model.HasResults, "HasResults");
        }

        [Test]
        public void CheckStateAfterUnloading()
        {
            _model.UnloadTests();

            Assert.False(_model.HasTests, "HasTests");
            Assert.Null(_model.LoadedTests, "Tests");
            Assert.False(_model.HasResults, "HasResults");
        }

        [Test]
        public void CheckStateAfterReloading()
        {
            _model.ReloadTests();

            Assert.That(_model.HasTests, "HasTests");
            Assert.NotNull(_model.LoadedTests, "Tests");
            Assert.False(_model.HasResults, "HasResults");
        }

        [Test]
        public void TestTreeIsUnchangedByReload()
        {
            var originalTests = _model.LoadedTests;

            _model.ReloadTests();

            Assert.Multiple(() => CheckNodesAreEqual(originalTests, _model.LoadedTests));
        }

        private void RunAllTestsAndWaitForCompletion()
        {
            bool runComplete = false;
            _model.Events.RunFinished += (r) => runComplete = true;

            _model.RunTests(_model.LoadedTests);

            while (!runComplete)
                System.Threading.Thread.Sleep(1);
        }

        private void CheckNodesAreEqual(TestNode beforeReload, TestNode afterReload)
        {
            Assert.That(afterReload.Name, Is.EqualTo(beforeReload.Name));
            Assert.That(afterReload.FullName, Is.EqualTo(beforeReload.FullName));
            Assert.That(afterReload.Id, Is.EqualTo(beforeReload.Id), $"Different IDs for {beforeReload.Name}");
            Assert.That(afterReload.IsSuite, Is.EqualTo(beforeReload.IsSuite));

            if (afterReload.IsSuite)
            {
                Assert.That(afterReload.Children.Count, Is.EqualTo(beforeReload.Children.Count), $"Different number of children for {beforeReload.Name}");
                for (int i = 0; i < afterReload.Children.Count; i++)
                    CheckNodesAreEqual(beforeReload.Children[i], afterReload.Children[i]);
            }
        }
    }
}
