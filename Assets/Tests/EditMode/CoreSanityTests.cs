using NUnit.Framework;
using RKW.Core;

namespace RKW.Core.Tests.EditMode
{
    public sealed class CoreSanityTests
    {
        [Test]
        public void ProjectIdentity_UsesDevelopmentPlaceholder()
        {
            Assert.That(ProjectIdentity.CompanyName, Is.EqualTo("Suite Digital"));
            Assert.That(ProjectIdentity.ProductName, Is.EqualTo("Project RKW"));
            Assert.That(
                ProjectIdentity.DevelopmentApplicationIdentifier,
                Does.EndWith(".dev"));
        }
    }
}
