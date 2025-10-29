using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void UI_ShouldNotDependOnInfrastructureDirectly()
        {
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoTspServer")
                .GetResult();

            // Використовуємо Constraint-based Assert
            Assert.That(result.IsSuccessful, Is.True);
        }
    }
}
