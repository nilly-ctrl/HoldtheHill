using System.Collections.Generic;
using HoldTheHill.Features.GameFlow;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class GameFlowControllerTests
    {
        private GameObject _object;
        private GameFlowController _flow;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("TestFlow");
            _flow = _object.AddComponent<GameFlowController>();
            // Awake does not run on AddComponent in edit mode, so the test starts the flow the way Awake would.
            typeof(GameFlowController).GetMethod("ForceState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_flow, new object[] { GameFlowState.Playing });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
            Time.timeScale = 1f;
        }

        [Test]
        public void Playing_RunsTimeAtNormalSpeed()
        {
            Assert.That(_flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(_flow.IsPlaying, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Pause_StopsTime_AndResume_BringsBackTheChosenSpeed()
        {
            _flow.SetSpeedIndex(1);
            Assert.That(Time.timeScale, Is.EqualTo(2f));

            _flow.Pause();
            Assert.That(_flow.State, Is.EqualTo(GameFlowState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(_flow.Speed, Is.EqualTo(2f), "the choice survives the pause");

            _flow.Resume();
            Assert.That(_flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(2f));
        }

        [Test]
        public void Resume_DoesNothing_UnlessPaused()
        {
            var seen = new List<(GameFlowState, GameFlowState)>();
            _flow.StateChanged += (previous, next) => seen.Add((previous, next));

            _flow.Resume();
            _flow.Pause();
            _flow.Pause();

            Assert.That(seen, Is.EqualTo(new[] { (GameFlowState.Playing, GameFlowState.Paused) }));
        }

        [Test]
        public void CycleSpeed_GoesRoundTheList()
        {
            Assert.That(_flow.SpeedCount, Is.EqualTo(3));

            _flow.CycleSpeed();
            _flow.CycleSpeed();
            Assert.That(_flow.Speed, Is.EqualTo(4f));

            _flow.CycleSpeed();
            Assert.That(_flow.Speed, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SpeedIndex_IsClamped()
        {
            _flow.SetSpeedIndex(99);
            Assert.That(_flow.SpeedIndex, Is.EqualTo(2));

            _flow.SetSpeedIndex(-5);
            Assert.That(_flow.SpeedIndex, Is.EqualTo(0));
        }

        [Test]
        public void SpeedOverride_ReplacesTheChosenSpeedWhilePlaying_AndClearsWithZero()
        {
            _flow.SetSpeedIndex(2);

            _flow.SetSpeedOverride(10f);
            Assert.That(Time.timeScale, Is.EqualTo(10f));

            _flow.Pause();
            Assert.That(Time.timeScale, Is.EqualTo(0f), "the override never unfreezes a pause");
            _flow.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(10f));

            _flow.SetSpeedOverride(0f);
            Assert.That(Time.timeScale, Is.EqualTo(4f), "back to the chosen speed");
        }

        [Test]
        public void SpeedOverride_IsClampedToTwenty()
        {
            _flow.SetSpeedOverride(500f);

            Assert.That(_flow.SpeedOverride, Is.EqualTo(20f));
        }
    }
}
