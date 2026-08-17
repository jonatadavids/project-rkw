using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;

namespace RKW.UI.Tests.EditMode
{
    public sealed class LocalizationFoundationTests
    {
        private const string InitializationWarning =
            "Localization initialization did not complete; emergency UI text is active.";

        private static readonly string[] ExpectedKeys =
        {
            "bootstrap.connecting",
            "bootstrap.connection_failed",
            "bootstrap.retry",
            "menu.play",
            "menu.school",
            "menu.garage",
            "menu.coming_soon"
        };

        [SetUp]
        public void SetUp()
        {
            UiLocalization.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            UiLocalization.ResetForTests();
        }

        [Test]
        public void Package_IsPinnedToApprovedVersion()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                "Packages/com.unity.localization");

            Assert.That(package, Is.Not.Null);
            Assert.That(package.version, Is.EqualTo("1.5.12"));
        }

        [Test]
        public void Settings_UsePtBrAndOnlyTechnicalEnglishFallback()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            var locales = LocalizationEditorSettings.GetLocales();
            var portuguese = LocalizationEditorSettings.GetLocale("pt-BR");
            var english = LocalizationEditorSettings.GetLocale("en");

            Assert.That(settings, Is.Not.Null);
            Assert.That(locales.Select(locale => locale.Identifier.Code),
                Is.EquivalentTo(new[] { "pt-BR", "en" }));
            Assert.That(settings.GetStartupLocaleSelectors(), Has.Count.EqualTo(1));
            Assert.That(settings.GetStartupLocaleSelectors()[0],
                Is.TypeOf<SpecificLocaleSelector>());
            Assert.That(((SpecificLocaleSelector)settings.GetStartupLocaleSelectors()[0])
                .LocaleId.Code, Is.EqualTo("pt-BR"));
            Assert.That(LocalizationSettings.ProjectLocale.Identifier.Code,
                Is.EqualTo("pt-BR"));
            Assert.That(LocalizationSettings.StringDatabase.UseFallback, Is.True);
            Assert.That(english.Metadata.GetMetadata<FallbackLocale>().Locale,
                Is.SameAs(portuguese));
        }

        [Test]
        public void UiCollection_ContainsOnlyConsumedPtBrEntries()
        {
            var collections = LocalizationEditorSettings.GetStringTableCollections();
            var collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            var portugueseTable = collection.GetTable("pt-BR") as StringTable;

            Assert.That(collections, Has.Count.EqualTo(1));
            Assert.That(collections[0].TableCollectionName, Is.EqualTo("UI"));
            Assert.That(collection, Is.Not.Null);
            Assert.That(collection.GetTable("en"), Is.Null);
            Assert.That(portugueseTable, Is.Not.Null);
            Assert.That(collection.SharedData.Entries.Select(entry => entry.Key),
                Is.EquivalentTo(ExpectedKeys));
            Assert.That(portugueseTable.Values, Has.Count.EqualTo(ExpectedKeys.Length));
            Assert.That(portugueseTable.Values.All(entry =>
                    !string.IsNullOrWhiteSpace(entry.LocalizedValue)),
                Is.True);
            Assert.That(collection.SharedData.GetEntry("KARTGRID"), Is.Null);
            Assert.That(collection.SharedData.GetEntry("PROJECT RKW • PROTÓTIPO DEV"),
                Is.Null);
        }

        [Test]
        public void UiTableAsset_LoadsAndReturnsReviewedStrings()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(
                "Assets/Localization/Tables/UI_pt-BR.asset");

            Assert.That(table, Is.Not.Null);
            Assert.That(table.GetEntry("bootstrap.connecting").LocalizedValue,
                Is.EqualTo("Conectando..."));
            Assert.That(table.GetEntry("menu.play").LocalizedValue,
                Is.EqualTo("JOGAR"));
        }

        [Test]
        public async Task InitializationPendingForever_TimesOutAndUsesOneSafeWarning()
        {
            var operations = new ControlledLocalizationOperations();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var initialization = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            operations.CompleteNextTimeout();
            await initialization;

            Assert.That(operations.InitializeCalls, Is.EqualTo(1));
            Assert.That(operations.PreloadCalls, Is.Zero);
            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(UiLocalization.Get(UiLocalization.MenuPlay),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            Assert.That(UiLocalization.Get(UiLocalization.MenuPlay),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task PreloadPendingForever_TimesOutAndUsesSafeFallback()
        {
            var operations = new ControlledLocalizationOperations();
            operations.CompleteInitialization();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var initialization = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            await operations.PreloadStarted.Task;
            operations.CompleteNextTimeout();
            await initialization;

            Assert.That(operations.InitializeCalls, Is.EqualTo(1));
            Assert.That(operations.PreloadCalls, Is.EqualTo(1));
            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(UiLocalization.Get(UiLocalization.BootstrapConnecting),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task InitializationCompletingLate_CannotReactivateLocalization()
        {
            var operations = new ControlledLocalizationOperations();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var initialization = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            operations.CompleteNextTimeout();
            await initialization;
            operations.CompleteInitialization();
            await Task.Yield();

            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(operations.PreloadCalls, Is.Zero);
            Assert.That(UiLocalization.Get(UiLocalization.MenuPlay),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task PreloadCompletingLate_CannotReactivateLocalization()
        {
            var operations = new ControlledLocalizationOperations();
            operations.CompleteInitialization();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var initialization = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            await operations.PreloadStarted.Task;
            operations.CompleteNextTimeout();
            await initialization;
            operations.CompletePreload();
            await Task.Yield();

            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(operations.PreloadCalls, Is.EqualTo(1));
            Assert.That(UiLocalization.Get(UiLocalization.MenuPlay),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task InitializationFailingLate_IsObservedWithoutAnotherWarning()
        {
            var operations = new ControlledLocalizationOperations();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var initialization = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            operations.CompleteNextTimeout();
            await initialization;
            operations.FailInitialization(new InvalidOperationException("sensitive detail"));
            await Task.Yield();

            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(operations.PreloadCalls, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task InitializationFailure_UsesSanitizedFallbackAndContinues()
        {
            var operations = new ControlledLocalizationOperations();
            operations.FailInitialization(new InvalidOperationException("sensitive detail"));
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            await UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));

            Assert.That(UiLocalization.IsAvailable, Is.False);
            Assert.That(operations.PreloadCalls, Is.Zero);
            Assert.That(UiLocalization.Get(UiLocalization.MenuPlay),
                Is.EqualTo(UiLocalization.EmergencyMessage));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task SharedInitialization_AllowsIndividualCallerCancellation()
        {
            var operations = new ControlledLocalizationOperations();
            using var cancellation = new CancellationTokenSource();
            LogAssert.Expect(LogType.Warning, InitializationWarning);

            var cancelledCaller = UiLocalization.InitializeAsync(
                cancellation.Token,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            var activeCaller = UiLocalization.InitializeAsync(
                CancellationToken.None,
                operations,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));

            cancellation.Cancel();
            try
            {
                await cancelledCaller;
                Assert.Fail("The individually cancelled caller must not complete successfully.");
            }
            catch (OperationCanceledException)
            {
                // Expected: the shared initialization remains active for the other caller.
            }

            operations.CompleteNextTimeout();
            await activeCaller;

            Assert.That(operations.InitializeCalls, Is.EqualTo(1));
            Assert.That(operations.DelayCalls, Is.EqualTo(1));
            Assert.That(UiLocalization.IsAvailable, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private sealed class ControlledLocalizationOperations : ILocalizationOperations
        {
            private readonly TaskCompletionSource<bool> _initialization = NewSignal();
            private readonly TaskCompletionSource<bool> _preload = NewSignal();
            private readonly List<TaskCompletionSource<bool>> _timeouts =
                new List<TaskCompletionSource<bool>>();

            internal TaskCompletionSource<bool> PreloadStarted { get; } = NewSignal();
            internal int InitializeCalls { get; private set; }
            internal int PreloadCalls { get; private set; }
            internal int DelayCalls { get; private set; }

            public Task InitializeAsync()
            {
                InitializeCalls++;
                return _initialization.Task;
            }

            public Task PreloadUiTableAsync()
            {
                PreloadCalls++;
                PreloadStarted.TrySetResult(true);
                return _preload.Task;
            }

            public Task DelayAsync(TimeSpan timeout)
            {
                DelayCalls++;
                var signal = NewSignal();
                _timeouts.Add(signal);
                return signal.Task;
            }

            internal void CompleteInitialization()
            {
                _initialization.TrySetResult(true);
            }

            internal void FailInitialization(Exception exception)
            {
                _initialization.TrySetException(exception);
            }

            internal void CompletePreload()
            {
                _preload.TrySetResult(true);
            }

            internal void CompleteNextTimeout()
            {
                Assert.That(_timeouts, Is.Not.Empty,
                    "The production code must create the stage timeout before the test releases it.");
                _timeouts[_timeouts.Count - 1].TrySetResult(true);
            }

            private static TaskCompletionSource<bool> NewSignal()
            {
                return new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
