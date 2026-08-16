using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class PlayModeSanityTests
    {
        [UnityTest]
        public IEnumerator PlayerLoop_AdvancesOneFrame()
        {
            yield return null;
            Assert.Pass("The PlayMode player loop advanced without errors.");
        }
    }
}
