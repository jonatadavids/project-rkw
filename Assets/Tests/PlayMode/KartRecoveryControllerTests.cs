using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class KartRecoveryControllerTests
    {
        [UnityTest]
        public IEnumerator InvertedKart_RecoversAtNearestPointWithZeroVelocity()
        {
            var kart = new GameObject("Recovery Test Kart");
            var body = kart.AddComponent<Rigidbody>();
            body.useGravity = false;
            kart.AddComponent<BoxCollider>();
            var recovery = kart.AddComponent<KartRecoveryController>();
            recovery.Configure(null,
                new List<Vector3> { new Vector3(10f, 0.5f, 0f), new Vector3(-10f, 0.5f, 0f) },
                new List<Vector3> { new Vector3(-20f, 0f, 0f), new Vector3(20f, 0f, 0f) }, 7f);

            body.position = new Vector3(8f, 0.5f, 1f);
            kart.transform.rotation = Quaternion.Euler(100f, 0f, 0f);
            body.linearVelocity = new Vector3(5f, 0f, 2f);
            body.angularVelocity = Vector3.one;
            UnityEngine.Physics.SyncTransforms();

            recovery.SendMessage("FixedUpdate");

            Assert.That(recovery.RecoveryCount, Is.EqualTo(1));
            Assert.That(recovery.LastRecoveryReason, Is.EqualTo(KartRecoveryReason.Inverted));
            Assert.That(Vector3.Distance(body.position, new Vector3(10f, 0.5f, 0f)), Is.LessThan(0.001f));
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.001f));
            Assert.That(body.angularVelocity.magnitude, Is.LessThan(0.001f));
            Assert.That(Vector3.Angle(body.rotation * Vector3.up, Vector3.up), Is.LessThan(0.1f));

            Object.Destroy(kart);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledRaceInput_PreventsFalseRecoveryDuringSetup()
        {
            var kart = new GameObject("Recovery Countdown Test Kart");
            var body = kart.AddComponent<Rigidbody>();
            body.useGravity = false;
            kart.AddComponent<BoxCollider>();
            kart.AddComponent<KartDynamics>();
            var input = kart.AddComponent<KartPrototypeInput>();
            input.SetInputEnabled(false);
            var recovery = kart.AddComponent<KartRecoveryController>();
            recovery.Configure(input, new List<Vector3> { Vector3.zero },
                new List<Vector3> { Vector3.left * 10f, Vector3.right * 10f }, 7f);
            kart.transform.rotation = Quaternion.Euler(100f, 0f, 0f);

            recovery.SendMessage("FixedUpdate");

            Assert.That(recovery.RecoveryCount, Is.Zero);

            Object.Destroy(kart);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Recovery_IgnoresOtherKartForConfiguredGraceThenRestoresCollision()
        {
            var kart = CreateKart("Recovery Grace Kart", out var body, out var ownCollider);
            var otherKart = CreateKart("Recovery Grace Other Kart", out _, out var otherCollider);
            var recovery = kart.AddComponent<KartRecoveryController>();
            recovery.Configure(null, new List<Vector3> { Vector3.zero },
                new List<Vector3> { Vector3.left * 10f, Vector3.right * 10f }, 7f,
                collisionGraceSeconds: 0.1f);
            kart.transform.rotation = Quaternion.Euler(100f, 0f, 0f);
            UnityEngine.Physics.SyncTransforms();

            recovery.SendMessage("FixedUpdate");

            Assert.That(recovery.IsCollisionGraceActive, Is.True);
            Assert.That(UnityEngine.Physics.GetIgnoreCollision(ownCollider, otherCollider), Is.True);

            yield return new WaitForSeconds(0.15f);
            recovery.SendMessage("FixedUpdate");

            Assert.That(recovery.IsCollisionGraceActive, Is.False);
            Assert.That(UnityEngine.Physics.GetIgnoreCollision(ownCollider, otherCollider), Is.False);

            Object.Destroy(kart);
            Object.Destroy(otherKart);
            yield return null;
        }

        private static GameObject CreateKart(string name, out Rigidbody body, out BoxCollider collider)
        {
            var kart = new GameObject(name);
            body = kart.AddComponent<Rigidbody>();
            body.useGravity = false;
            collider = kart.AddComponent<BoxCollider>();
            kart.AddComponent<KartDynamics>();
            return kart;
        }
    }
}
