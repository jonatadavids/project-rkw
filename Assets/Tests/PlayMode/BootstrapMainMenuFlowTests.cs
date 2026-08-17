using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Backend;
using RKW.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RKW.Tests.PlayMode
{
    public sealed class BootstrapMainMenuFlowTests
    {
        private const int TestTimeoutSeconds = 30;

        [TearDown]
        public void TearDown()
        {
            BootstrapController.ResetTestOverrides();
        }

        [UnityTest]
        public IEnumerator SuccessfulAuthentication_LoadsMainMenuAdditivelyOnce()
        {
            BootstrapController.AuthenticationFactoryOverride =
                () => new StubAuthenticationService(true);

            yield return LoadBootstrapAndWaitForMainMenu();

            Assert.That(SceneManager.GetSceneByName("Bootstrap").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(CountLoadedScenesNamed("MainMenu"), Is.EqualTo(1));
            AssertExactlyOneActiveCamera();

            AssertButton("PlayButton", "JOGAR");
            AssertButton("SchoolButton", "ESCOLA");
            AssertButton("GarageButton", "GARAGEM");
            Assert.That(GameObject.Find("Title").GetComponent<Text>().text, Is.EqualTo("KARTGRID"));
            Assert.That(GameObject.Find("DevelopmentLabel").GetComponent<Text>().text,
                Is.EqualTo("PROJECT RKW • PROTÓTIPO DEV"));
        }

        [UnityTest]
        public IEnumerator FailedAuthentication_ShowsSafeMessage_AndRetryRecovers()
        {
            var authentication = new StubAuthenticationService(false, true);
            BootstrapController.AuthenticationFactoryOverride = () => authentication;

            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntil(() => GameObject.Find("RetryButton")?.activeInHierarchy == true);

            Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.False);
            Assert.That(GameObject.Find("StatusText").GetComponent<Text>().text,
                Is.EqualTo("Não foi possível conectar. Tente novamente."));

            var retryButton = GameObject.Find("RetryButton").GetComponent<Button>();
            for (var click = 0; click < 5; click++)
            {
                retryButton.onClick.Invoke();
            }

            yield return WaitUntil(() => SceneManager.GetSceneByName("MainMenu").isLoaded);

            Assert.That(authentication.CallCount, Is.EqualTo(2));
            Assert.That(CountLoadedScenesNamed("MainMenu"), Is.EqualTo(1));
            AssertExactlyOneActiveCamera();
        }

        [UnityTest]
        public IEnumerator RepeatedBootstrapToMainMenuLoads_KeepExactlyOneActiveCamera()
        {
            BootstrapController.AuthenticationFactoryOverride =
                () => new StubAuthenticationService(true);

            yield return LoadBootstrapAndWaitForMainMenu();
            AssertExactlyOneActiveCamera();

            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntil(() =>
                SceneManager.GetSceneByName("MainMenu").isLoaded
                && SceneManager.GetActiveScene().name == "MainMenu");

            Assert.That(CountLoadedScenesNamed("MainMenu"), Is.EqualTo(1));
            AssertExactlyOneActiveCamera();
        }

        [UnityTest]
        public IEnumerator DestroyingBootstrapDuringPendingAuthentication_CancelsWithoutLeaks()
        {
            var authentication = new PendingAuthenticationService();
            BootstrapController.AuthenticationFactoryOverride = () => authentication;
            var unobservedExceptionCount = 0;
            System.EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, eventArgs) =>
            {
                unobservedExceptionCount++;
                eventArgs.SetObserved();
            };
            TaskScheduler.UnobservedTaskException += handler;

            try
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
                yield return WaitUntil(() => authentication.CallCount == 1);

                var hostScene = SceneManager.CreateScene("BootstrapDestructionTestHost");
                SceneManager.SetActiveScene(hostScene);
                yield return SceneManager.UnloadSceneAsync("Bootstrap");
                yield return WaitUntil(() => authentication.CancellationObserved);
                yield return null;
                yield return null;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                yield return null;

                Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.False);
                Assert.That(
                    UnityEngine.Object.FindObjectsByType<BootstrapController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Is.Empty);
                Assert.That(GameObject.Find("Application"), Is.Null);
                Assert.That(unobservedExceptionCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }
        }

        [UnityTest]
        public IEnumerator PlaceholderButtons_DoNotNavigate_AndShowComingSoon()
        {
            BootstrapController.AuthenticationFactoryOverride =
                () => new StubAuthenticationService(true);
            yield return LoadBootstrapAndWaitForMainMenu();

            var initialSceneCount = SceneManager.sceneCount;
            foreach (var buttonName in new[] { "PlayButton", "SchoolButton", "GarageButton" })
            {
                GameObject.Find(buttonName).GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
                Assert.That(GameObject.Find("FeedbackText").GetComponent<Text>().text,
                    Is.EqualTo(MainMenuController.ComingSoonText));
                Assert.That(GameObject.Find("FeedbackText").activeInHierarchy, Is.True);
            }
        }

        [TestCase(2340f, 1080f, 80f, 0f, 2260f, 1080f)]
        [TestCase(2556f, 1179f, 132f, 63f, 2424f, 1116f)]
        public void SafeAreaAnchors_StayNormalizedForTargetLandscapeRatios(
            float width,
            float height,
            float x,
            float y,
            float safeWidth,
            float safeHeight)
        {
            SafeAreaFitter.CalculateNormalizedAnchors(
                new Rect(x, y, safeWidth, safeHeight),
                new Vector2(width, height),
                out var minimum,
                out var maximum);

            Assert.That(minimum.x, Is.InRange(0f, 1f));
            Assert.That(minimum.y, Is.InRange(0f, 1f));
            Assert.That(maximum.x, Is.InRange(0f, 1f));
            Assert.That(maximum.y, Is.InRange(0f, 1f));
            Assert.That(maximum.x, Is.GreaterThan(minimum.x));
            Assert.That(maximum.y, Is.GreaterThan(minimum.y));
        }

        [UnityTest]
        public IEnumerator RealDevelopmentAuthentication_LoadsMainMenu()
        {
            if (!EnvironmentFlagIsSet("RKW_RUN_M1_T06_UGS"))
            {
                Assert.Ignore(
                    "Set RKW_RUN_M1_T06_UGS=1 to validate the real UGS development bootstrap.");
            }

            BootstrapController.ResetTestOverrides();
            yield return LoadBootstrapAndWaitForMainMenu();

            Assert.That(SceneManager.GetSceneByName("Bootstrap").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureDevelopmentMainMenuWithoutSensitiveData()
        {
            if (!EnvironmentFlagIsSet("RKW_CAPTURE_M1_T06"))
            {
                Assert.Ignore(
                    "Set RKW_CAPTURE_M1_T06=1 to generate the reviewed menu screenshot.");
            }

            BootstrapController.AuthenticationFactoryOverride =
                () => new StubAuthenticationService(true);
            yield return LoadBootstrapAndWaitForMainMenu();
            yield return null;

            const int width = 2340;
            const int height = 1080;
            const string outputPath = "/tmp/rkw-m1-t06-main-menu.png";
            var canvas = GameObject.Find("MainMenuView").GetComponent<Canvas>();
            var captureCamera = Camera.main;
            Assert.That(captureCamera, Is.Not.Null);
            AssertExactlyOneActiveCamera();
            var originalCullingMask = captureCamera.cullingMask;
            captureCamera.cullingMask = ~0;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = 1f;

            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            RenderTexture.active = null;
            captureCamera.targetTexture = null;
            captureCamera.cullingMask = originalCullingMask;
            canvas.worldCamera = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(image);

            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(1024));
        }

        private static IEnumerator LoadBootstrapAndWaitForMainMenu()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntil(() =>
                SceneManager.GetSceneByName("MainMenu").isLoaded
                && SceneManager.GetActiveScene().name == "MainMenu");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && stopwatch.Elapsed.TotalSeconds < TestTimeoutSeconds)
            {
                yield return null;
            }

            Assert.That(
                condition(),
                Is.True,
                $"Condition did not complete within {TestTimeoutSeconds} seconds.");
        }

        private static int CountLoadedScenesNamed(string sceneName)
        {
            var count = 0;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).name == sceneName)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertExactlyOneActiveCamera()
        {
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].gameObject.name, Is.EqualTo("Bootstrap Camera"));
            Assert.That(cameras[0].gameObject.scene.name, Is.EqualTo("Bootstrap"));
            Assert.That(cameras[0].cullingMask, Is.Zero);
        }

        private static bool EnvironmentFlagIsSet(string variableName)
        {
            return string.Equals(
                Environment.GetEnvironmentVariable(variableName),
                "1",
                StringComparison.Ordinal);
        }

        private static void AssertButton(string name, string expectedLabel)
        {
            var buttonObject = GameObject.Find(name);
            Assert.That(buttonObject, Is.Not.Null);
            Assert.That(buttonObject.GetComponent<Button>().interactable, Is.True);
            Assert.That(buttonObject.GetComponentInChildren<Text>().text, Is.EqualTo(expectedLabel));
            Assert.That(((RectTransform)buttonObject.transform).rect.height, Is.GreaterThanOrEqualTo(96f));
        }

        private sealed class StubAuthenticationService : IAuthenticationService
        {
            private readonly bool[] _results;

            public StubAuthenticationService(params bool[] results)
            {
                _results = results;
            }

            public bool IsSignedIn { get; private set; }
            public int CallCount { get; private set; }

            public Task<bool> SignInAnonymouslyAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = _results[Mathf.Min(CallCount, _results.Length - 1)];
                CallCount++;
                IsSignedIn = result;
                return Task.FromResult(result);
            }
        }

        private sealed class PendingAuthenticationService : IAuthenticationService
        {
            private readonly TaskCompletionSource<bool> _completion =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool IsSignedIn => false;
            public int CallCount { get; private set; }
            public bool CancellationObserved { get; private set; }

            public Task<bool> SignInAnonymouslyAsync(
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                cancellationToken.Register(() =>
                {
                    CancellationObserved = true;
                    _completion.TrySetCanceled(cancellationToken);
                });
                return _completion.Task;
            }
        }
    }
}
