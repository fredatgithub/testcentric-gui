// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System;
using TestCentric.Engine.Runners;
using NUnit.Framework;
using NUnit.Engine;

namespace TestCentric.Engine.Services
{
    //using Fakes;

    public class InProcessTestRunnerFactoryTests
    {
        private InProcessTestRunnerFactory _factory;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            _factory = new InProcessTestRunnerFactory();
            services.Add(_factory);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_factory.Status, Is.EqualTo(ServiceStatus.Started), "Failed to start service");
        }

#if NETCOREAPP1_1 || NETCOREAPP2_1
        [TestCase("x.dll", null, typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll", null, typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll z.dll", null, typeof(LocalTestRunner))]
#else
        // Single file
        [TestCase("x.dll",  null,      typeof(TestDomainRunner))]
        [TestCase("x.dll", "Single",   typeof(TestDomainRunner))]
        [TestCase("x.dll", "Multiple", typeof(TestDomainRunner))]
        // Two files
        [TestCase("x.dll y.dll",  null,     typeof(MultipleTestDomainRunner))]
        [TestCase("x.dll y.dll", "Single",   typeof(TestDomainRunner))]
        [TestCase("x.dll y.dll", "Multiple", typeof(MultipleTestDomainRunner))]
        // Three files
        [TestCase("x.dll y.dll z.dll", null,       typeof(MultipleTestDomainRunner))]
        [TestCase("x.dll y.dll z.dll", "Single",   typeof(TestDomainRunner))]
        [TestCase("x.dll y.dll z.dll", "Multiple", typeof(MultipleTestDomainRunner))]
#endif
        public void CorrectRunnerIsUsed(string files, string domainUsage, Type expectedType)
        {
            var package = new TestPackage(files.Split(new char[] { ' ' }));
            if (domainUsage != null)
                package.Settings["DomainUsage"] = domainUsage;
            Assert.That(_factory.MakeTestRunner(package), Is.TypeOf(expectedType));
        }
    }
}
