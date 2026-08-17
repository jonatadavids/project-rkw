using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Backend;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.EditMode
{
    public sealed class CloudPersistenceContractTests
    {
        private const string ValidJson = "{\"schemaVersion\":1,\"coins\":0}";

        [Test]
        public void SaveJsonAsync_RejectsMissingKeys()
        {
            var persistence = CreatePersistence(new FakeCloudDataClient());
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync(null, ValidJson, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync(string.Empty, ValidJson, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync(" profile", ValidJson, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync("profile ", ValidJson, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync("profile\nvalue", ValidJson, CancellationToken.None));
        }

        [Test]
        public void LoadJsonAsync_RejectsMissingKeys()
        {
            var persistence = CreatePersistence(new FakeCloudDataClient());
            Assert.ThrowsAsync<ArgumentException>(() => persistence.LoadJsonAsync(null, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.LoadJsonAsync(string.Empty, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.LoadJsonAsync(" profile", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.LoadJsonAsync("profile ", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.LoadJsonAsync("profile\tvalue", CancellationToken.None));
        }

        [Test]
        public void SaveJsonAsync_RejectsMissingPayloads()
        {
            var persistence = CreatePersistence(new FakeCloudDataClient());
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync("profile", null, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync("profile", string.Empty, CancellationToken.None));
        }

        [TestCase("not-json")]
        [TestCase("{")]
        [TestCase("{\"value\":}")]
        public void SaveJsonAsync_RejectsSyntacticallyInvalidJsonBeforeSdkCall(string invalidJson)
        {
            var client = new FakeCloudDataClient();
            var persistence = CreatePersistence(client);
            Assert.ThrowsAsync<ArgumentException>(() => persistence.SaveJsonAsync("profile", invalidJson, CancellationToken.None));
            Assert.That(client.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task SaveAndLoad_PreserveExactJsonString()
        {
            const string exactJson = "{ \"schemaVersion\" : 1, \"name\" : \"Kart\\nRKW\" }";
            var client = new FakeCloudDataClient { LoadOperation = _ => Task.FromResult(exactJson) };
            var persistence = CreatePersistence(client);
            await persistence.SaveJsonAsync("profile", exactJson, CancellationToken.None);
            var loaded = await persistence.LoadJsonAsync("profile", CancellationToken.None);
            Assert.That(client.LastSavedJson, Is.EqualTo(exactJson));
            Assert.That(loaded, Is.EqualTo(exactJson));
        }

        [Test]
        public void SaveJsonAsync_AcceptsPayloadAtDefensiveBudget()
        {
            var persistence = CreatePersistence(new FakeCloudDataClient());
            var payload = CreateJsonPayloadOfExactly(UgsCloudPersistence.MaxJsonPayloadBytes);
            Assert.That(Encoding.UTF8.GetByteCount(payload), Is.EqualTo(UgsCloudPersistence.MaxJsonPayloadBytes));
            Assert.DoesNotThrowAsync(() => persistence.SaveJsonAsync("profile", payload, CancellationToken.None));
        }

        [Test]
        public void SaveJsonAsync_RejectsPayloadBeyondDefensiveBudget()
        {
            var persistence = CreatePersistence(new FakeCloudDataClient());
            var payload = CreateJsonPayloadOfExactly(UgsCloudPersistence.MaxJsonPayloadBytes + 1);
            Assert.That(Encoding.UTF8.GetByteCount(payload), Is.EqualTo(UgsCloudPersistence.MaxJsonPayloadBytes + 1));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                persistence.SaveJsonAsync("profile", payload, CancellationToken.None));
        }

        [Test]
        public void Authentication_AlwaysRequestsDevelopmentEnvironment()
        {
            var client = new FakeAuthenticationClient();
            var service = CreateAuthentication(client);
            Assert.That(service.SignInAnonymouslyAsync(CancellationToken.None).GetAwaiter().GetResult(), Is.True);
            Assert.That(client.RequestedEnvironment, Is.EqualTo("development"));
            Assert.That(client.AuthenticationCalls, Is.EqualTo(1));
        }

        [Test]
        public void Authentication_RejectsAnAlreadyInitializedUnknownEnvironment()
        {
            var client = new FakeAuthenticationClient { ServicesInitialized = true };
            var service = CreateAuthentication(client);
            LogAssert.Expect(LogType.Warning, "UGS development anonymous authentication failed (InvalidOperationException).");
            Assert.That(service.SignInAnonymouslyAsync(CancellationToken.None).GetAwaiter().GetResult(), Is.False);
            Assert.That(client.InitializeCalls, Is.Zero);
            Assert.That(client.AuthenticationCalls, Is.Zero);
        }

        [Test]
        public void Authentication_SdkFailureIsHandledWithoutCrash()
        {
            var client = new FakeAuthenticationClient
            {
                ServicesInitialized = true,
                DevelopmentEnvironmentConfirmed = true,
                AuthenticationOperation = () => Task.FromException(new InvalidOperationException("deterministic failure"))
            };
            var service = CreateAuthentication(client);
            LogAssert.Expect(LogType.Warning, "UGS development anonymous authentication failed (InvalidOperationException).");
            Assert.That(service.SignInAnonymouslyAsync(CancellationToken.None).GetAwaiter().GetResult(), Is.False);
            Assert.That(client.AuthenticationCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task Authentication_InitializationTimeoutIsFiniteAndConfigurable()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeAuthenticationClient { InitializationOperation = () => pending.Task };
            var service = CreateAuthentication(client, TimeSpan.FromMilliseconds(50));
            await ExpectExceptionAsync<TimeoutException>(
                service.SignInAnonymouslyAsync(CancellationToken.None));
            Assert.That(client.RequestedEnvironment, Is.EqualTo("development"));
            Assert.That(client.AuthenticationCalls, Is.Zero);
            pending.TrySetResult(true);
            await Task.Yield();
        }

        [Test]
        public async Task Authentication_SignInTimeoutIsFiniteAndConfigurable()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeAuthenticationClient
            {
                ServicesInitialized = true,
                DevelopmentEnvironmentConfirmed = true,
                AuthenticationOperation = () => pending.Task
            };
            var service = CreateAuthentication(client, TimeSpan.FromMilliseconds(50));
            await ExpectExceptionAsync<TimeoutException>(
                service.SignInAnonymouslyAsync(CancellationToken.None));
            Assert.That(client.AuthenticationCalls, Is.EqualTo(1));
            pending.TrySetResult(true);
            await Task.Yield();
        }

        [Test]
        public async Task Authentication_CancellationDuringPendingInitializationReturnsPromptly()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeAuthenticationClient { InitializationOperation = () => pending.Task };
            var service = CreateAuthentication(client, TimeSpan.FromSeconds(5));
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = service.SignInAnonymouslyAsync(cancellation.Token);
                Assert.That(operation.IsCompleted, Is.False);
                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(operation);
                Assert.That(pending.Task.IsCompleted, Is.False);
                pending.TrySetException(new InvalidOperationException("late deterministic failure"));
                await Task.Delay(10);
            }
        }

        [Test]
        public async Task Authentication_CancellationDuringPendingSdkOperationReturnsPromptlyAndObservesLateFailure()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeAuthenticationClient
            {
                ServicesInitialized = true,
                DevelopmentEnvironmentConfirmed = true,
                AuthenticationOperation = () => pending.Task
            };
            var service = CreateAuthentication(client, TimeSpan.FromSeconds(5));
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = service.SignInAnonymouslyAsync(cancellation.Token);
                Assert.That(operation.IsCompleted, Is.False);
                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(operation);
                Assert.That(pending.Task.IsCompleted, Is.False);
                pending.TrySetException(new InvalidOperationException("late deterministic failure"));
                await Task.Delay(10);
            }
        }

        [Test]
        public async Task SaveJsonAsync_TimesOutWhileSdkOperationRemainsPending()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeCloudDataClient { SaveOperation = (_, __) => pending.Task };
            var persistence = CreatePersistence(client, TimeSpan.FromMilliseconds(50));
            await ExpectExceptionAsync<TimeoutException>(
                persistence.SaveJsonAsync("profile", ValidJson, CancellationToken.None));
            Assert.That(pending.Task.IsCompleted, Is.False);
            pending.TrySetResult(true);
            await Task.Yield();
        }

        [Test]
        public async Task LoadJsonAsync_TimesOutWhileSdkOperationRemainsPending()
        {
            var pending = NewCompletionSource<string>();
            var client = new FakeCloudDataClient { LoadOperation = _ => pending.Task };
            var persistence = CreatePersistence(client, TimeSpan.FromMilliseconds(50));
            await ExpectExceptionAsync<TimeoutException>(
                persistence.LoadJsonAsync("profile", CancellationToken.None));
            Assert.That(pending.Task.IsCompleted, Is.False);
            pending.TrySetResult(ValidJson);
            await Task.Yield();
        }

        [Test]
        public async Task SaveJsonAsync_CancellationReturnsPromptlyWhileSubmittedWriteMayComplete()
        {
            var pending = NewCompletionSource<bool>();
            var client = new FakeCloudDataClient { SaveOperation = (_, __) => pending.Task };
            var persistence = CreatePersistence(client, TimeSpan.FromSeconds(5));
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = persistence.SaveJsonAsync("profile", ValidJson, cancellation.Token);
                Assert.That(operation.IsCompleted, Is.False);
                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(operation);
                Assert.That(pending.Task.IsCompleted, Is.False);

                pending.TrySetResult(true);
                await pending.Task;
                Assert.That(pending.Task.IsCompletedSuccessfully, Is.True);
            }
        }

        [Test]
        public async Task LoadJsonAsync_CancellationDuringPendingSdkOperationReturnsPromptlyAndObservesLateFailure()
        {
            var pending = NewCompletionSource<string>();
            var client = new FakeCloudDataClient { LoadOperation = _ => pending.Task };
            var persistence = CreatePersistence(client, TimeSpan.FromSeconds(5));
            using (var cancellation = new CancellationTokenSource())
            {
                var operation = persistence.LoadJsonAsync("profile", cancellation.Token);
                Assert.That(operation.IsCompleted, Is.False);
                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(operation);
                Assert.That(pending.Task.IsCompleted, Is.False);
                pending.TrySetException(new InvalidOperationException("late deterministic failure"));
                await Task.Delay(10);
            }
        }

        [Test]
        public void SaveJsonAsync_PropagatesDeterministicSdkFailure()
        {
            var client = new FakeCloudDataClient
            {
                SaveOperation = (_, __) => Task.FromException(new InvalidOperationException("deterministic failure"))
            };
            var persistence = CreatePersistence(client);
            Assert.ThrowsAsync<InvalidOperationException>(() => persistence.SaveJsonAsync("profile", ValidJson, CancellationToken.None));
        }

        [Test]
        public void SaveAndLoad_RejectCallsUnlessDevelopmentAuthenticationIsConfirmed()
        {
            var client = new FakeCloudDataClient { AuthenticatedForDevelopment = false };
            var persistence = CreatePersistence(client);
            Assert.ThrowsAsync<InvalidOperationException>(() => persistence.SaveJsonAsync("profile", ValidJson, CancellationToken.None));
            Assert.ThrowsAsync<InvalidOperationException>(() => persistence.LoadJsonAsync("profile", CancellationToken.None));
            Assert.That(client.SaveCalls, Is.Zero);
            Assert.That(client.LoadCalls, Is.Zero);
        }

        [Test]
        public void Operations_HonorPreCanceledTokensWithoutCallingSdk()
        {
            var authClient = new FakeAuthenticationClient();
            var authentication = CreateAuthentication(authClient);
            var cloudClient = new FakeCloudDataClient();
            var persistence = CreatePersistence(cloudClient);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.CatchAsync<OperationCanceledException>(() => authentication.SignInAnonymouslyAsync(cancellation.Token));
                Assert.CatchAsync<OperationCanceledException>(() => persistence.SaveJsonAsync("profile", ValidJson, cancellation.Token));
                Assert.CatchAsync<OperationCanceledException>(() => persistence.LoadJsonAsync("profile", cancellation.Token));
            }
            Assert.That(authClient.InitializeCalls, Is.Zero);
            Assert.That(cloudClient.SaveCalls, Is.Zero);
            Assert.That(cloudClient.LoadCalls, Is.Zero);
        }

        [Test]
        public void UgsOperationTimeouts_RejectsInfiniteZeroOrUnsupportedDurations()
        {
            var valid = TimeSpan.FromSeconds(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new UgsOperationTimeouts(TimeSpan.Zero, valid, valid, valid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UgsOperationTimeouts(valid, Timeout.InfiniteTimeSpan, valid, valid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UgsOperationTimeouts(valid, valid, TimeSpan.FromDays(30), valid));
        }

        private static UgsAuthenticationService CreateAuthentication(FakeAuthenticationClient client, TimeSpan? timeout = null)
        {
            var duration = timeout ?? TimeSpan.FromSeconds(1);
            return new UgsAuthenticationService(client, new UgsOperationTimeouts(duration, duration, duration, duration));
        }

        private static UgsCloudPersistence CreatePersistence(FakeCloudDataClient client, TimeSpan? timeout = null)
        {
            var duration = timeout ?? TimeSpan.FromSeconds(1);
            return new UgsCloudPersistence(client, new UgsOperationTimeouts(duration, duration, duration, duration));
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

        private static string CreateJsonPayloadOfExactly(int targetBytes)
        {
            const string prefix = "{\"data\":\"";
            const string suffix = "\"}";
            return prefix + new string('a', targetBytes - prefix.Length - suffix.Length) + suffix;
        }

        private sealed class FakeAuthenticationClient : IUgsAuthenticationClient
        {
            public bool ServicesInitialized { get; set; }
            public bool DevelopmentEnvironmentConfirmed { get; set; }
            public bool SignedIn { get; set; }
            public int InitializeCalls { get; private set; }
            public int AuthenticationCalls { get; private set; }
            public string RequestedEnvironment { get; private set; }
            public Func<Task> InitializationOperation { get; set; } = () => Task.CompletedTask;
            public Func<Task> AuthenticationOperation { get; set; } = () => Task.CompletedTask;

            bool IUgsAuthenticationClient.IsServicesInitialized => ServicesInitialized;
            bool IUgsAuthenticationClient.IsDevelopmentEnvironmentConfirmed => DevelopmentEnvironmentConfirmed;
            bool IUgsAuthenticationClient.IsSignedIn => SignedIn;

            public async Task InitializeAsync(string environmentName)
            {
                InitializeCalls++;
                RequestedEnvironment = environmentName;
                await InitializationOperation();
                ServicesInitialized = true;
                DevelopmentEnvironmentConfirmed = string.Equals(environmentName, "development", StringComparison.Ordinal);
            }

            public async Task SignInAnonymouslyAsync()
            {
                AuthenticationCalls++;
                await AuthenticationOperation();
                SignedIn = true;
            }
        }

        private sealed class FakeCloudDataClient : IUgsCloudDataClient
        {
            public bool AuthenticatedForDevelopment { get; set; } = true;
            public int SaveCalls { get; private set; }
            public int LoadCalls { get; private set; }
            public string LastSavedJson { get; private set; }
            public Func<string, string, Task> SaveOperation { get; set; } = (_, __) => Task.CompletedTask;
            public Func<string, Task<string>> LoadOperation { get; set; } = _ => Task.FromResult(ValidJson);

            bool IUgsCloudDataClient.IsAuthenticatedForDevelopment => AuthenticatedForDevelopment;

            public Task SaveJsonAsync(string key, string json)
            {
                SaveCalls++;
                LastSavedJson = json;
                return SaveOperation(key, json);
            }

            public Task<string> LoadJsonAsync(string key)
            {
                LoadCalls++;
                return LoadOperation(key);
            }
        }
    }
}
