using HoldTheHill.Features.Combat;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class ProjectileFlightTests
    {
        private static ProjectileFlight.Settings Settings(FlightMode mode) => new ProjectileFlight.Settings
        {
            Mode = mode,
            Speed = 10f,
            TurnRate = 90f,
            Lifetime = 5f,
            ArcHeight = 2f,
            LoiterDuration = 1f,
            DiveSpeedMultiplier = 2.5f,
            LoiterBobAmplitude = 0.25f,
        };

        [Test]
        public void Linear_FliesStraightAtSpeed_AndExpiresAfterItsLifetime()
        {
            var flight = new ProjectileFlight();
            flight.Launch(Settings(FlightMode.Linear), Vector2.zero, null, Vector2.up);

            flight.Advance(0.5f, null);

            Assert.That(flight.Position, Is.EqualTo(new Vector2(0f, 5f)));
            Assert.That(flight.Facing, Is.EqualTo(Vector2.up));
            Assert.That(flight.HasExpired, Is.False);

            flight.Advance(4.5f, null);
            Assert.That(flight.HasExpired, Is.True);
            Assert.That(flight.Position, Is.EqualTo(new Vector2(0f, 5f)), "an expired shot does not move");
        }

        [Test]
        public void Launch_AimsAtTheTarget_ElseAlongTheDirection_ElseRight()
        {
            var flight = new ProjectileFlight();

            flight.Launch(Settings(FlightMode.Linear), Vector2.zero, new Vector2(0f, 3f), Vector2.right);
            Assert.That(flight.Direction, Is.EqualTo(Vector2.up));

            flight.Launch(Settings(FlightMode.Linear), Vector2.zero, null, Vector2.left);
            Assert.That(flight.Direction, Is.EqualTo(Vector2.left));

            flight.Launch(Settings(FlightMode.Linear), Vector2.zero, null, Vector2.zero);
            Assert.That(flight.Direction, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void Homing_TurnsTowardTheTarget_NoFasterThanItsTurnRate()
        {
            var flight = new ProjectileFlight();
            flight.Launch(Settings(FlightMode.Homing), Vector2.zero, null, Vector2.right);
            Assert.That(flight.WantsTarget(0.1f), Is.True);

            // 90 degrees per second, half a second: 45 degrees toward a target straight up.
            flight.Advance(0.5f, new Vector2(0f, 100f));

            float angle = Vector2.SignedAngle(Vector2.right, flight.Direction);
            Assert.That(angle, Is.EqualTo(45f).Within(0.01f));
            Assert.That(flight.Position.magnitude, Is.EqualTo(5f).Within(0.001f));

            Vector2 before = flight.Direction;
            flight.Advance(0.5f, null);
            Assert.That(flight.Direction, Is.EqualTo(before), "with no target it keeps its heading");
        }

        [Test]
        public void Lobbed_RisesAboveTheLine_AndLandsOnTheTarget()
        {
            var flight = new ProjectileFlight();
            var landing = new Vector2(10f, 0f);
            flight.Launch(Settings(FlightMode.Lobbed), Vector2.zero, landing, Vector2.right);
            Assert.That(flight.WantsTarget(0.1f), Is.False);

            // 10 units at speed 10: one second in the air; halfway is the apex, two units up.
            flight.Advance(0.5f, null);
            Assert.That(flight.ArcProgress, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(flight.Position.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(flight.Position.y, Is.EqualTo(2f).Within(0.001f));
            Assert.That(flight.HasLanded, Is.False);

            flight.Advance(0.5f, null);
            Assert.That(flight.HasLanded, Is.True);
            Assert.That(flight.Position, Is.EqualTo(landing));

            flight.Advance(0.5f, null);
            Assert.That(flight.Position, Is.EqualTo(landing), "a landed shot stays put");
        }

        [Test]
        public void SeekAndDestroy_HoversThroughTheLoiter_ThenDivesAtTheTarget()
        {
            var flight = new ProjectileFlight();
            var origin = new Vector2(3f, 3f);
            flight.Launch(Settings(FlightMode.SeekAndDestroy), origin, null, Vector2.right);

            flight.Advance(0.25f, null);
            Assert.That(flight.IsDiving, Is.False);
            Assert.That(flight.Position.x, Is.EqualTo(origin.x));
            Assert.That(Mathf.Abs(flight.Position.y - origin.y), Is.LessThanOrEqualTo(0.26f), "it bobs near its origin");
            Assert.That(flight.WantsTarget(0.25f), Is.False, "not until the loiter is over");

            flight.Advance(0.5f, null);
            Assert.That(flight.WantsTarget(0.25f), Is.True, "the frame it commits it asks for a target");

            // 2.5 times speed 10 for a quarter second: 6.25 units toward the target.
            flight.Advance(0.25f, new Vector2(-100f, 3f));
            Assert.That(flight.IsDiving, Is.True);
            Assert.That(flight.Direction.x, Is.EqualTo(-1f).Within(0.001f), "it dives toward the target");
            Assert.That(flight.Direction.y, Is.EqualTo(0f).Within(0.01f), "almost level: the bob left it a quarter unit low");
            Assert.That(flight.Position.x, Is.EqualTo(3f - 6.25f).Within(0.001f));
            Assert.That(flight.WantsTarget(0.25f), Is.False, "once diving it keeps the target it has");
        }

        [Test]
        public void Instant_NeverMoves()
        {
            var flight = new ProjectileFlight();
            flight.Launch(Settings(FlightMode.Instant), Vector2.one, null, Vector2.up);

            flight.Advance(1f, new Vector2(5f, 5f));

            Assert.That(flight.Position, Is.EqualTo(Vector2.one));
            Assert.That(flight.WantsTarget(1f), Is.False);
        }
    }
}
