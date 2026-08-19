using NUnit.Framework;
using UnityEngine;

namespace RKW.Track.Tests.EditMode
{
    /// <summary>
    /// M3-T03: Criar TrackConditionSO "Dry".
    /// Validates: Requirement 18.5 (MVP — "Seco" is a true neutral baseline;
    /// dry condition does not alter grip, multipliers = 1.0).
    /// </summary>
    public sealed class TrackConditionSOTests
    {
        private const string DryResourcePath = "Track/DryTrackCondition";

        [Test]
        public void DryCondition_LoadsFromResources()
        {
            var condition = Resources.Load<TrackConditionSO>(DryResourcePath);

            Assert.That(condition, Is.Not.Null,
                $"Expected a TrackConditionSO asset at Resources/{DryResourcePath}.asset");
        }

        [Test]
        public void DryCondition_IsValid()
        {
            var condition = Resources.Load<TrackConditionSO>(DryResourcePath);
            Assert.That(condition, Is.Not.Null);

            var isValid = condition.IsValid(out var reason);

            Assert.That(isValid, Is.True, $"Dry condition should be valid, but: {reason}");
        }

        [Test]
        public void DryCondition_DoesNotAlterGrip()
        {
            var condition = Resources.Load<TrackConditionSO>(DryResourcePath);
            Assert.That(condition, Is.Not.Null);

            Assert.That(condition.LongitudinalGripMultiplier, Is.EqualTo(1f));
            Assert.That(condition.LateralGripMultiplier, Is.EqualTo(1f));
            Assert.That(condition.BrakingDistanceMultiplier, Is.EqualTo(1f));
            Assert.That(condition.TractionMultiplier, Is.EqualTo(1f));
            Assert.That(condition.CurbGripMultiplier, Is.EqualTo(1f));
            Assert.That(condition.GrassGripMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void DryCondition_IsNeutralBaseline()
        {
            var condition = Resources.Load<TrackConditionSO>(DryResourcePath);
            Assert.That(condition, Is.Not.Null);

            Assert.That(condition.IsNeutralBaseline(), Is.True,
                "Dry must be a true no-op condition: no puddles, spray, particles, " +
                "or grip/traction/braking deviation from 1.0.");
        }

        [Test]
        public void NonNeutralCondition_IsNotFlaggedAsNeutralBaseline()
        {
            var condition = ScriptableObject.CreateInstance<TrackConditionSO>();
            var serialized = new UnityEditor.SerializedObject(condition);
            serialized.FindProperty("lateralGripMultiplier").floatValue = 0.7f;
            serialized.FindProperty("conditionId").stringValue = "heavy-rain";
            serialized.FindProperty("displayName").stringValue = "Chuva forte";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(condition.IsNeutralBaseline(), Is.False);
            Assert.That(condition.IsValid(out _), Is.True);

            Object.DestroyImmediate(condition);
        }
    }
}
