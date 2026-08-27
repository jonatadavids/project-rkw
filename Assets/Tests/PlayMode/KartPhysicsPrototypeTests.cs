using System.Collections;
using System.Linq;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class KartPhysicsPrototypeTests
    {
        [UnityTest]
        public IEnumerator Prototype_RemainsStableAndOwnsExactlyOneCamera()
        {
            var scene = SceneManager.CreateScene("KartPhysicsPrototypeTest");
            SceneManager.SetActiveScene(scene);
            var bootstrapObject = new GameObject("Test Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<KartPhysicsPrototypeBootstrap>();
            yield return null;

            // Round 36: Awake() now only shows TrackSelectMenu and waits for
            // a real tap before building anything -- OnTrackSelected(true)
            // simulates picking Carrera Kart (Circuit2), the same track this
            // test exercised by default before track choice became a
            // runtime menu instead of a build-time constant.
            bootstrap.OnTrackSelected(true);
            yield return null;

            var kart = bootstrap.SpawnedKart;
            Assert.That(kart, Is.Not.Null);
            kart.GetComponent<KartPrototypeInput>().enabled = false;
            var initialRotation = kart.transform.rotation;
            kart.SetInput(0.45f, 1f, 0f);

            for (var tick = 0; tick < 75; tick++)
            {
                yield return new WaitForFixedUpdate();
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(camera => camera.isActiveAndEnabled)
                .ToArray();
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(kart.SpeedKph, Is.GreaterThan(1f));
            Assert.That(Quaternion.Angle(initialRotation, kart.transform.rotation), Is.GreaterThan(1f));
            Assert.That(float.IsFinite(kart.transform.position.x), Is.True);
            Assert.That(float.IsFinite(kart.transform.position.y), Is.True);
            Assert.That(float.IsFinite(kart.transform.position.z), Is.True);
            Assert.That(kart.transform.position.y, Is.InRange(-0.5f, 2f));

            kart.SetInput(0f, 0f, 1f);
            for (var tick = 0; tick < 150; tick++)
            {
                yield return new WaitForFixedUpdate();
            }

            var localVelocity = kart.transform.InverseTransformDirection(kart.GetComponent<Rigidbody>().linearVelocity);
            Assert.That(localVelocity.z, Is.LessThan(-0.1f), "Brake must become limited reverse after stopping.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
