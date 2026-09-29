using GalacticRupture.Sim;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    public class SimAssemblyTests
    {
        [Test]
        public void SimAssemblyHasNoEngineReference()
        {
            foreach (var reference in typeof(SimInfo).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name, Does.Not.StartWith("UnityEngine"));
        }
    }
}
