using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Tests.PlayMode
{
    public sealed class TimingManagerLiteValidationTests
    {
        private GameObject _timingObject;
        private TimingManagerLite _timing;

        [SetUp]
        public void SetUp()
        {
            _timingObject = new GameObject("TimingManagerLite Test");
            _timing = _timingObject.AddComponent<TimingManagerLite>();
            _timing.Configure(3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_timingObject);
        }

        [Test]
        public void CompleteOrderedLap_IncrementsValidLapCount()
        {
            StartLap();
            PassAllCheckpointsInOrder();

            _timing.RegisterCheckpointHit(0, true, true);

            Assert.That(_timing.LapsCompleted, Is.EqualTo(1));
            Assert.That(_timing.BestLapTime, Is.LessThan(float.MaxValue));
        }

        [Test]
        public void ReverseFinishCrossing_DoesNotStartOrCompleteLap()
        {
            var completionEvents = 0;
            _timing.OnLapCompleted += (_, _) => completionEvents++;

            _timing.RegisterCheckpointHit(0, true, false);

            Assert.That(_timing.LapsCompleted, Is.Zero);
            Assert.That(_timing.CurrentLapTime, Is.Zero);
            Assert.That(completionEvents, Is.Zero);
        }

        [Test]
        public void ReverseFinishAfterAllCheckpoints_PreservesProgressUntilForwardCrossing()
        {
            StartLap();
            PassAllCheckpointsInOrder();

            _timing.RegisterCheckpointHit(0, true, false);
            Assert.That(_timing.LapsCompleted, Is.Zero);

            _timing.RegisterCheckpointHit(0, true, true);
            Assert.That(_timing.LapsCompleted, Is.EqualTo(1));
        }

        [Test]
        public void ForwardFinishWithoutFullLap_ReportsInvalidButDoesNotIncrementLapCount()
        {
            var completionEvents = 0;
            var invalidationEvents = 0;
            _timing.OnLapCompleted += (_, _) => completionEvents++;
            _timing.OnLapInvalidated += () => invalidationEvents++;
            StartLap();

            _timing.RegisterCheckpointHit(0, true, true);

            Assert.That(completionEvents, Is.Zero);
            Assert.That(invalidationEvents, Is.EqualTo(1));
            Assert.That(_timing.LapsCompleted, Is.Zero);
            Assert.That(_timing.LastLapTime, Is.Zero);
            Assert.That(_timing.BestLapTime, Is.EqualTo(float.MaxValue));
        }

        [Test]
        public void WrongCheckpointOrder_DoesNotIncrementLapCount()
        {
            StartLap();
            _timing.RegisterCheckpointHit(1, false);
            _timing.RegisterCheckpointHit(0, false);
            _timing.RegisterCheckpointHit(2, false);

            _timing.RegisterCheckpointHit(0, true, true);

            Assert.That(_timing.LapsCompleted, Is.Zero);
        }

        [Test]
        public void StartFinishDirection_UsesVelocityAndRejectsReverseMotion()
        {
            var triggerObject = new GameObject("StartFinish Test");
            triggerObject.AddComponent<BoxCollider>().isTrigger = true;
            var trigger = triggerObject.AddComponent<CheckpointTrigger>();
            trigger.Configure(0, true, Vector3.right);

            Assert.That(trigger.IsCrossingForward(Vector3.right * 5f, Vector3.left), Is.True);
            Assert.That(trigger.IsCrossingForward(Vector3.left * 5f, Vector3.right), Is.False);

            Object.DestroyImmediate(triggerObject);
        }

        private void StartLap()
        {
            _timing.RegisterCheckpointHit(0, true, true);
        }

        private void PassAllCheckpointsInOrder()
        {
            _timing.RegisterCheckpointHit(0, false);
            _timing.RegisterCheckpointHit(1, false);
            _timing.RegisterCheckpointHit(2, false);
        }
    }
}
