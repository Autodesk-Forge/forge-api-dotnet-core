using System;
using Xunit;

namespace Autodesk.Forge.Core.Test
{
    public class TestForgeService
    {
        [Fact]
        public void TestDefault()
        {
            var svc = ForgeService.CreateDefault();
            Assert.NotNull(svc);
            Assert.NotNull(svc.Client);
        }

        [Fact]
        public void TestNullClientThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>new ForgeService(null));
        }
    }
}
