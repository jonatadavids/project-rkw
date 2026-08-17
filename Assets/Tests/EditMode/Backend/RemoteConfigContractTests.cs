using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Backend;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.EditMode
{
    public sealed class RemoteConfigContractTests
    {
        [Test]
        public void SafeDefaults_DisableEveryApprovedFeature()
        {
            var flags = RemoteFeatureFlags.SafeDefaults;

            Assert.That(flags.EnableMultiplayer, Is.False);
            Assert.That(flags.EnableChampionship, Is.False);
            Assert.That(flags.EnableSchool, Is.False);
            Assert.That(flags.EnableAds, Is.False);
        }

        [Test]
        public async Task DevelopmentFetch_ExposesOnlyTheApprovedFlagValues()
        {
            var expected = new RemoteFeatureFlags(true, true, false, true);
            var client = new FakeRemoteConfigClient { FetchOperation = () => Task.FromResult(expected) };
            var manager = new RemoteConfigManager(client, TimeSpan.FromSeconds(1));

            var flags = await manager.LoadAsync(CancellationToken.None);

            Assert.That(client.FetchCalls, Is.EqualTo(1));
            Assert.That(flags.EnableMultiplayer, Is.True);
            Assert.That(flags.EnableChampionship, Is.True);
            Assert.That(flags.EnableSchool, Is.False);
            Assert.That(flags.EnableAds, Is.True);
            Assert.That(manager.Flags.EnableMultiplayer, Is.True);
        }

        [Test]
        public async Task UnconfirmedEnvironment_DoesNotCallSdkAndUsesSafeDefaults()
        {
            var client = new FakeRemoteConfigClient { AuthenticatedForDevelopment = false };
            var manager = new RemoteConfigManager(client, TimeSpan.FromSeconds(1));
            LogAssert.Expect(
                LogType.Warning,
                "UGS development Remote Config unavailable (authentication was not confirmed); local feature defaults remain active.");

            var flags = await manager.LoadAsync(CancellationToken.None);

            Assert.That(client.FetchCalls, Is.Zero);
            AssertSafeDefaults(flags);
        }

        [Test]
        public async Task SdkFailure_UsesSafeDefaultsWithoutThrowing()
        {
            var client = new FakeRemoteConfigClient
            {
                FetchOperation = () => Task.FromException<RemoteFeatureFlags>(
                    new InvalidOperationException("deterministic failure"))
            };
            var manager = new RemoteConfigManager(client, TimeSpan.FromSeconds(1));
            LogAssert.Expect(
                LogType.Warning,
                "UGS development Remote Config unavailable (InvalidOperationException); local feature defaults remain active.");

            var flags = await manager.LoadAsync(CancellationToken.None);

            Assert.That(client.FetchCalls, Is.EqualTo(1));
            AssertSafeDefaults(flags);
        }

        [Test]
        public async Task PendingFetch_TimesOutAndUsesSafeDefaults()
        {
            var pending = NewCompletionSource<RemoteFeatureFlags>();
            var client = new FakeRemoteConfigClient { FetchOperation = () => pending.Task };
            var manager = new RemoteConfigManager(client, TimeSpan.FromMilliseconds(50));
            LogAssert.Expect(
                LogType.Warning,
                "UGS development Remote Config unavailable (TimeoutException); local feature defaults remain active.");

            var flags = await manager.LoadAsync(CancellationToken.None);

            Assert.That(pending.Task.IsCompleted, Is.False);
            AssertSafeDefaults(flags);
            pending.TrySetResult(new RemoteFeatureFlags(true, true, true, true));
            await Task.Yield();
            AssertSafeDefaults(manager.Flags);
        }

        [Test]
        public async Task CallerCancellation_ReturnsPromptlyAndObservesLateFailure()
        {
            var pending = NewCompletionSource<RemoteFeatureFlags>();
            var client = new FakeRemoteConfigClient { FetchOperation = () => pending.Task };
            var manager = new RemoteConfigManager(client, TimeSpan.FromSeconds(5));

            using (var cancellation = new CancellationTokenSource())
            {
                var operation = manager.LoadAsync(cancellation.Token);
                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(operation);
                Assert.That(pending.Task.IsCompleted, Is.False);
                pending.TrySetException(new InvalidOperationException("late deterministic failure"));
                await Task.Delay(10);
            }
        }

        [Test]
        public async Task ConcurrentCallers_ShareOneRemoteFetch()
        {
            var pending = NewCompletionSource<RemoteFeatureFlags>();
            var client = new FakeRemoteConfigClient { FetchOperation = () => pending.Task };
            var manager = new RemoteConfigManager(client, TimeSpan.FromSeconds(1));

            var first = manager.LoadAsync(CancellationToken.None);
            var second = manager.LoadAsync(CancellationToken.None);
            Assert.That(client.FetchCalls, Is.EqualTo(1));
            pending.TrySetResult(new RemoteFeatureFlags(false, true, false, false));

            await Task.WhenAll(first, second);
            Assert.That(manager.Flags.EnableChampionship, Is.True);
        }

        private static void AssertSafeDefaults(RemoteFeatureFlags flags)
        {
            Assert.That(flags.EnableMultiplayer, Is.False);
            Assert.That(flags.EnableChampionship, Is.False);
            Assert.That(flags.EnableSchool, Is.False);
            Assert.That(flags.EnableAds, Is.False);
        }

        private static TaskCompletionSource<T> NewCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static async Task<TException> ExpectExceptionAsync<TException>(Task operation)
            where TException : Exception
        {
            try
            {
                await operation;
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail($"Expected {typeof(TException).Name}, but the operation completed successfully.");
            return null;
        }

        private sealed class FakeRemoteConfigClient : IRemoteConfigClient
        {
            public bool AuthenticatedForDevelopment { get; set; } = true;
            public int FetchCalls { get; private set; }
            public Func<Task<RemoteFeatureFlags>> FetchOperation { get; set; } =
                () => Task.FromResult(RemoteFeatureFlags.SafeDefaults);

            bool IRemoteConfigClient.IsAuthenticatedForDevelopment => AuthenticatedForDevelopment;

            public Task<RemoteFeatureFlags> FetchAsync()
            {
                FetchCalls++;
                return FetchOperation();
            }
        }
    }
}
