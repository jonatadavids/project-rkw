using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Backend;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class UgsFoundationIntegrationTests
    {
        private const string SmokeKey = "rkw_m1_t05_smoke_v1";
        private const int IntegrationTimeoutSeconds = 30;

        [UnityTest]
        public IEnumerator DevelopmentAnonymousAuth_SavesAndLoadsExactJson()
        {
            if (!ShouldRunUgsIntegration())
            {
                Assert.Ignore(
                    "Set RKW_RUN_UGS_INTEGRATION=1 to run the UGS development integration test.");
            }

            var authentication = new UgsAuthenticationService();
            var authenticationTask = authentication.SignInAnonymouslyAsync();
            yield return WaitFor(authenticationTask, IntegrationTimeoutSeconds);

            Assert.That(authenticationTask.Result, Is.True);
            Assert.That(authentication.IsSignedIn, Is.True);

            var expected = new CloudSaveSmokePayload(
                $"development-{Guid.NewGuid():N}",
                CloudSaveSmokePayload.CurrentSchemaVersion);
            var expectedJson = expected.ToJson();
            ICloudPersistence persistence = new UgsCloudPersistence();

            var saveTask = persistence.SaveJsonAsync(SmokeKey, expectedJson);
            yield return WaitFor(saveTask, IntegrationTimeoutSeconds);

            var loadTask = persistence.LoadJsonAsync(SmokeKey);
            yield return WaitFor(loadTask, IntegrationTimeoutSeconds);

            Assert.That(loadTask.Result, Is.EqualTo(expectedJson));
            Assert.That(
                CloudSaveSmokePayload.FromJson(loadTask.Result),
                Is.EqualTo(expected));

            UnityEngine.Debug.Log("UGS development Cloud Save JSON round-trip succeeded.");
        }

        private static IEnumerator WaitFor(Task task, int timeoutSeconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!task.IsCompleted && stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.True, $"UGS operation exceeded {timeoutSeconds} seconds.");
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        private static bool ShouldRunUgsIntegration()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("RKW_RUN_UGS_INTEGRATION"),
                "1",
                StringComparison.Ordinal);
        }
    }
}
