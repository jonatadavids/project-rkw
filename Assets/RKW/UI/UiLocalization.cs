using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RKW.UI
{
    internal static class UiLocalization
    {
        internal static readonly TimeSpan DefaultInitializationTimeout = TimeSpan.FromSeconds(10);
        internal static readonly TimeSpan DefaultPreloadTimeout = TimeSpan.FromSeconds(10);

        internal const string TableName = "UI";
        internal const string BootstrapConnecting = "bootstrap.connecting";
        internal const string BootstrapConnectionFailed = "bootstrap.connection_failed";
        internal const string BootstrapRetry = "bootstrap.retry";
        internal const string MenuPlay = "menu.play";
        internal const string MenuSchool = "menu.school";
        internal const string MenuGarage = "menu.garage";
        internal const string MenuComingSoon = "menu.coming_soon";

        // Infrastructure-only fallback used when Localization cannot provide any text.
        internal const string EmergencyMessage = "Texto indisponível.";

        private static readonly object Gate = new object();
        private static readonly HashSet<string> ReportedFailures =
            new HashSet<string>(StringComparer.Ordinal);

        private static Task _initializationTask;
        private static bool _initializationFinished;
        private static bool _isAvailable;
        private static bool _diagnosticsSubscribed;

        internal static double InitializationDurationMilliseconds { get; private set; }
        internal static long InitializationMemoryDeltaBytes { get; private set; }
        internal static bool IsAvailable => _isAvailable;

        internal static Task InitializeAsync(CancellationToken cancellationToken)
        {
            return InitializeAsync(
                cancellationToken,
                UnityLocalizationOperations.Instance,
                DefaultInitializationTimeout,
                DefaultPreloadTimeout);
        }

        internal static Task InitializeAsync(
            CancellationToken cancellationToken,
            ILocalizationOperations operations,
            TimeSpan initializationTimeout,
            TimeSpan preloadTimeout)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            ValidateTimeout(initializationTimeout, nameof(initializationTimeout));
            ValidateTimeout(preloadTimeout, nameof(preloadTimeout));

            Task initialization;
            lock (Gate)
            {
                initialization = _initializationTask ??= InitializeCoreAsync(
                    operations,
                    initializationTimeout,
                    preloadTimeout);
            }

            return AwaitForCallerAsync(initialization, cancellationToken);
        }

        internal static string Get(string key)
        {
            if (!_isAvailable)
            {
                if (_initializationFinished)
                {
                    ReportInitializationFailureOnce();
                }

                return EmergencyMessage;
            }

            try
            {
                var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    TableName,
                    key);
                var value = operation.IsDone ? operation.Result : operation.WaitForCompletion();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch (Exception)
            {
                // The safe fallback below is intentionally independent of Localization.
            }

            ReportOnce(key, $"Localization key '{key}' could not be resolved; emergency UI text is active.");
            return EmergencyMessage;
        }

        private static async Task InitializeCoreAsync(
            ILocalizationOperations operations,
            TimeSpan initializationTimeout,
            TimeSpan preloadTimeout)
        {
            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();

            try
            {
                await WaitForStageAsync(
                    operations.InitializeAsync(),
                    operations.DelayAsync(initializationTimeout));

                SubscribeToMissingTranslationDiagnostics();
                await WaitForStageAsync(
                    operations.PreloadUiTableAsync(),
                    operations.DelayAsync(preloadTimeout));

                _isAvailable = true;
            }
            catch (Exception)
            {
                _isAvailable = false;
                ReportInitializationFailureOnce();
            }
            finally
            {
                stopwatch.Stop();
                InitializationDurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                InitializationMemoryDeltaBytes =
                    UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() - memoryBefore;
                _initializationFinished = true;
            }
        }

        private static async Task WaitForStageAsync(Task operation, Task timeout)
        {
            if (operation == null)
            {
                throw new InvalidOperationException("Localization operation was not created.");
            }

            if (timeout == null)
            {
                throw new InvalidOperationException("Localization timeout was not created.");
            }

            var completed = await Task.WhenAny(operation, timeout);
            if (completed == operation)
            {
                await operation;
                return;
            }

            await timeout;
            ObserveLateCompletion(operation);
            throw new TimeoutException("Localization stage exceeded its configured timeout.");
        }

        private static void ObserveLateCompletion(Task operation)
        {
            _ = operation.ContinueWith(
                completed =>
                {
                    _ = completed.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static void ValidateTimeout(TimeSpan timeout, string parameterName)
        {
            if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Localization timeout must be finite and greater than zero.");
            }
        }

        private static async Task AwaitForCallerAsync(
            Task initialization,
            CancellationToken cancellationToken)
        {
            if (initialization.IsCompleted)
            {
                await initialization;
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }

            var cancellation = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(
                       () => cancellation.TrySetResult(true),
                       useSynchronizationContext: false))
            {
                if (await Task.WhenAny(initialization, cancellation.Task) != initialization)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            await initialization;
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static void SubscribeToMissingTranslationDiagnostics()
        {
            if (_diagnosticsSubscribed)
            {
                return;
            }

            LocalizationSettings.StringDatabase.TranslationNotFound +=
                HandleMissingTranslation;
            _diagnosticsSubscribed = true;
        }

        private static void HandleMissingTranslation(
            string key,
            long keyId,
            TableReference tableReference,
            StringTable table,
            UnityEngine.Localization.Locale locale,
            string message)
        {
            ReportOnce(key, $"Localization key '{key}' is missing; safe fallback text was returned.");
        }

        private static void ReportInitializationFailureOnce()
        {
            ReportOnce(
                "localization-initialization",
                "Localization initialization did not complete; emergency UI text is active.");
        }

        internal static void ResetForTests()
        {
            lock (Gate)
            {
                _initializationTask = null;
                _initializationFinished = false;
                _isAvailable = false;
                InitializationDurationMilliseconds = 0;
                InitializationMemoryDeltaBytes = 0;
                ReportedFailures.Clear();
            }
        }

        private static void ReportOnce(string identifier, string message)
        {
            lock (Gate)
            {
                if (!ReportedFailures.Add(identifier))
                {
                    return;
                }
            }

            UnityEngine.Debug.LogWarning(message);
        }
    }

    internal interface ILocalizationOperations
    {
        Task InitializeAsync();

        Task PreloadUiTableAsync();

        Task DelayAsync(TimeSpan timeout);
    }

    internal sealed class UnityLocalizationOperations : ILocalizationOperations
    {
        internal static readonly UnityLocalizationOperations Instance =
            new UnityLocalizationOperations();

        private UnityLocalizationOperations()
        {
        }

        public async Task InitializeAsync()
        {
            var operation = LocalizationSettings.InitializationOperation;
            await operation.Task;
            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Localization initialization did not succeed.");
            }
        }

        public async Task PreloadUiTableAsync()
        {
            var operation = LocalizationSettings.StringDatabase.PreloadTables(UiLocalization.TableName);
            await operation.Task;
            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("UI String Table preload did not succeed.");
            }
        }

        public Task DelayAsync(TimeSpan timeout)
        {
            return Task.Delay(timeout);
        }
    }
}
