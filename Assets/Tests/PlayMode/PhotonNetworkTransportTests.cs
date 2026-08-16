using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Network;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class PhotonNetworkTransportTests
    {
        [UnityTest]
        public IEnumerator CancelledAttempt_DoesNotLeaveRunner()
        {
            var owner = new GameObject("Photon cancellation test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var task = transport.ConnectAsync("rkw-cancel-test", 1f, cancellation.Token);
            yield return WaitFor(task);

            Assert.That(task.IsCompletedSuccessfully, Is.True);
            Assert.That(task.Result.Status, Is.EqualTo(NetworkConnectionStatus.Cancelled));
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);
            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisconnectWhileStartGameIsPending_CannotReturnConnectedOrLeaveRunner()
        {
            var owner = new GameObject("Photon pending disconnect test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var startEntered = new TaskCompletionSource<bool>();
            var releaseStart = new TaskCompletionSource<bool>();
            transport.StartGameOverride = _ =>
            {
                startEntered.TrySetResult(true);
                return releaseStart.Task;
            };

            var connectTask = transport.ConnectAsync("rkw-pending-disconnect-test", 30f);
            yield return WaitFor(startEntered.Task);

            var disconnectTask = transport.DisconnectAsync();
            Assert.That(disconnectTask.IsCompleted, Is.False, "Disconnect must coordinate with pending startup.");
            releaseStart.TrySetResult(true);

            yield return WaitFor(connectTask);
            yield return WaitFor(disconnectTask);
            yield return null;

            Assert.That(connectTask.Result.Status, Is.EqualTo(NetworkConnectionStatus.Cancelled));
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);

            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancellationWhileStartGameIsPending_CannotReturnConnectedOrLeaveRunner()
        {
            var owner = new GameObject("Photon pending cancellation test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var startEntered = new TaskCompletionSource<bool>();
            var releaseStart = new TaskCompletionSource<bool>();
            transport.StartGameOverride = _ =>
            {
                startEntered.TrySetResult(true);
                return releaseStart.Task;
            };

            using var cancellation = new CancellationTokenSource();
            var connectTask = transport.ConnectAsync("rkw-pending-cancellation-test", 30f, cancellation.Token);
            yield return WaitFor(startEntered.Task);

            cancellation.Cancel();
            releaseStart.TrySetResult(true);

            yield return WaitFor(connectTask);
            yield return null;

            Assert.That(connectTask.Result.Status, Is.EqualTo(NetworkConnectionStatus.Cancelled));
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);

            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyWhileStartGameIsPending_CannotReturnConnectedOrLeaveRunner()
        {
            var owner = new GameObject("Photon pending destruction test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var startEntered = new TaskCompletionSource<bool>();
            var releaseStart = new TaskCompletionSource<bool>();
            transport.StartGameOverride = _ =>
            {
                startEntered.TrySetResult(true);
                return releaseStart.Task;
            };

            var connectTask = transport.ConnectAsync("rkw-pending-destruction-test", 30f);
            yield return WaitFor(startEntered.Task);

            UnityEngine.Object.Destroy(owner);
            yield return null;
            releaseStart.TrySetResult(true);

            yield return WaitFor(connectTask);
            yield return null;

            Assert.That(connectTask.Result.Status, Is.EqualTo(NetworkConnectionStatus.Cancelled));
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MissingLocalConfiguration_ReturnsWithoutCrashOrOrphanRunner()
        {
            if (ShouldRunPhotonIntegration())
            {
                Assert.Ignore("This failure-path test runs with the committed blank local configuration.");
            }

            var owner = new GameObject("Photon missing configuration test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var task = transport.ConnectAsync("rkw-missing-config-test", 2f);
            yield return WaitFor(task);
            yield return null;

            Assert.That(task.IsCompletedSuccessfully, Is.True);
            Assert.That(task.Result.IsSuccess, Is.False);
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);

            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimedOutAttempt_ShutsDownWithoutOrphanRunner()
        {
            if (!ShouldRunPhotonIntegration())
            {
                Assert.Ignore("Set RKW_RUN_PHOTON_INTEGRATION=1 to run the local Photon integration test.");
            }

            var owner = new GameObject("Photon timeout test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var task = transport.ConnectAsync($"rkw-timeout-{Guid.NewGuid():N}", 0.001f);
            yield return WaitFor(task);
            yield return null;

            Assert.That(task.IsCompletedSuccessfully, Is.True);
            Assert.That(task.Result.Status, Is.EqualTo(NetworkConnectionStatus.TimedOut), task.Result.Reason);
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);

            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DevelopmentConnection_ConnectsAndShutsDownTwiceWithoutOrphanRunner()
        {
            if (!ShouldRunPhotonIntegration())
            {
                Assert.Ignore("Set RKW_RUN_PHOTON_INTEGRATION=1 to run the local Photon integration test.");
            }

            var owner = new GameObject("Photon development connection test");
            var transport = owner.AddComponent<PhotonNetworkTransport>();
            var sessionName = $"rkw-m1-t04-{Guid.NewGuid():N}";

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var connectTask = transport.ConnectAsync(sessionName, 30f);
                yield return WaitFor(connectTask);

                Assert.That(connectTask.IsCompletedSuccessfully, Is.True);
                Assert.That(connectTask.Result.Status, Is.EqualTo(NetworkConnectionStatus.Connected), connectTask.Result.Reason);
                Assert.That(transport.IsConnected, Is.True);

                var disconnectTask = transport.DisconnectAsync();
                yield return WaitFor(disconnectTask);
                yield return null;

                Assert.That(disconnectTask.IsCompletedSuccessfully, Is.True);
                Assert.That(transport.IsConnected, Is.False);
                Assert.That(PhotonNetworkTransport.ActiveRunnerCount, Is.Zero);
            }

            UnityEngine.Object.Destroy(owner);
            yield return null;
        }

        private static IEnumerator WaitFor(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        private static bool ShouldRunPhotonIntegration()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("RKW_RUN_PHOTON_INTEGRATION"),
                "1",
                StringComparison.Ordinal);
        }
    }
}
