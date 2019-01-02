using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    public class OrderAttribute : Attribute
    {
        public double Weight { get; set; }
    }

    public class TestOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            var ordered = testCases.OrderBy(test => test.TestMethod.Method.ToRuntimeMethod().GetCustomAttribute<OrderAttribute>().Weight);
            return ordered;
        }
    }
}
