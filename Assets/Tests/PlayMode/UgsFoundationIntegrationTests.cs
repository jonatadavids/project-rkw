using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Backend;
using UnityEngine;
using Unity.Services.RemoteConfig;
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

        [UnityTest]
        public IEnumerator DevelopmentRemoteConfig_FetchesApprovedFlagAllowList()
        {
            if (!ShouldRunRemoteConfigIntegration())
            {
                Assert.Ignore(
                    "Set RKW_RUN_M1_T11_REMOTE_CONFIG=1 to run the Remote Config development integration test.");
            }

            var authentication = new UgsAuthenticationService();
            var authenticationTask = authentication.SignInAnonymouslyAsync();
            yield return WaitFor(authenticationTask, IntegrationTimeoutSeconds);

            Assert.That(authenticationTask.Result, Is.True);
            LogAssert.Expect(LogType.Log, "UGS development Remote Config fetch succeeded.");
            var remoteConfig = new RemoteConfigManager();
            var fetchTask = remoteConfig.LoadAsync();
            yield return WaitFor(fetchTask, IntegrationTimeoutSeconds);

            Assert.That(fetchTask.IsCompletedSuccessfully, Is.True);
            Assert.That(fetchTask.Result.EnableMultiplayer,
                Is.False);
            Assert.That(fetchTask.Result.EnableChampionship,
                Is.False);
            Assert.That(fetchTask.Result.EnableSchool,
                Is.False);
            Assert.That(fetchTask.Result.EnableAds,
                Is.False);
            Assert.That(RemoteConfigService.Instance.appConfig.HasKey(
                    RemoteFeatureFlags.EnableMultiplayerKey),
                Is.True);
            Assert.That(RemoteConfigService.Instance.appConfig.HasKey(
                    RemoteFeatureFlags.EnableChampionshipKey),
                Is.True);
            Assert.That(RemoteConfigService.Instance.appConfig.HasKey(
                    RemoteFeatureFlags.EnableSchoolKey),
                Is.True);
            Assert.That(RemoteConfigService.Instance.appConfig.HasKey(
                    RemoteFeatureFlags.EnableAdsKey),
                Is.True);
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

        private static bool ShouldRunRemoteConfigIntegration()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("RKW_RUN_M1_T11_REMOTE_CONFIG"),
                "1",
                StringComparison.Ordinal);
        }
    }
}
