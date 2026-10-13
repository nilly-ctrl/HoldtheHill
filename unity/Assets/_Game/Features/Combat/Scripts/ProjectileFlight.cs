using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Where a shot is and where it is going: the flight maths of a <see cref="Projectile"/>, kept
    /// out of the scene so EditMode tests can fly every mode. Each frame the projectile hands it
    /// the frame time and its target's position, then copies <see cref="Position"/> and
    /// <see cref="Facing"/> onto its transform.
    /// </summary>
    public sealed class ProjectileFlight
    {
        /// <summary>The numbers a flight needs, read off the projectile's serialized fields.</summary>
        public struct Settings
        {
            public FlightMode Mode;
            public float Speed;
            public float TurnRate;
            public float Lifetime;
            public float ArcHeight;
            public float LoiterDuration;
            public float DiveSpeedMultiplier;
            public float LoiterBobAmplitude;
        }

        private Settings _settings;
        private Vector2 _launchPoint;
        private Vector2 _landingPoint;
        private float _arcDuration = 1f;
        private Vector2 _loiterOrigin;

        /// <summary>How this shot travels.</summary>
        public FlightMode Mode => _settings.Mode;

        /// <summary>Where the shot is now.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Direction of travel. A lobbed shot keeps its launch direction; see <see cref="Facing"/>.</summary>
        public Vector2 Direction { get; private set; } = Vector2.right;

        /// <summary>Which way the sprite should point: the way the shot moved this frame.</summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        /// <summary>Seconds since launch.</summary>
        public float Age { get; private set; }

        /// <summary>Progress along a lobbed arc, 0 at launch and 1 on landing.</summary>
        public float ArcProgress { get; private set; }

        /// <summary>Seek-and-destroy only: true once the shot has committed to its dive.</summary>
        public bool IsDiving { get; private set; }

        /// <summary>Lobbed only: true once the shot sits at its landing point.</summary>
        public bool HasLanded { get; private set; }

        /// <summary>True once the shot has outlived its lifetime.</summary>
        public bool HasExpired => Age >= _settings.Lifetime;

        /// <summary>Lobbed only: where the arc comes down.</summary>
        public Vector2 LandingPoint => _landingPoint;

        /// <summary>
        /// True when the next <see cref="Advance"/> should be given a live target: a homing shot
        /// always wants one, a seek-and-destroy shot the frame it commits to its dive.
        /// </summary>
        public bool WantsTarget(float dt)
        {
            switch (_settings.Mode)
            {
                case FlightMode.Homing:
                    return true;
                case FlightMode.SeekAndDestroy:
                    return !IsDiving && Age + dt >= _settings.LoiterDuration;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Starts a flight at <paramref name="origin"/>. Aims at the target when there is one,
        /// else along <paramref name="direction"/>; a zero direction points right.
        /// </summary>
        public void Launch(Settings settings, Vector2 origin, Vector2? targetPosition, Vector2 direction)
        {
            _settings = settings;
            Position = origin;
            Age = 0f;
            ArcProgress = 0f;
            IsDiving = false;
            HasLanded = false;

            Vector2 toTarget = targetPosition.HasValue ? targetPosition.Value - origin : direction;
            Direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
            Facing = Direction;

            switch (settings.Mode)
            {
                case FlightMode.Lobbed:
                    _launchPoint = origin;
                    _landingPoint = targetPosition ?? origin + direction;
                    _arcDuration = Mathf.Max(0.1f, Vector2.Distance(_launchPoint, _landingPoint) / Mathf.Max(0.01f, settings.Speed));
                    break;

                case FlightMode.SeekAndDestroy:
                    _loiterOrigin = origin;
                    break;
            }
        }

        /// <summary>
        /// Moves the shot by one frame. An expired shot does not move, a landed lobbed shot stays
        /// put, and an instant shot never moves at all.
        /// </summary>
        public void Advance(float dt, Vector2? targetPosition)
        {
            Age += dt;
            if (HasExpired || HasLanded)
            {
                return;
            }

            switch (_settings.Mode)
            {
                case FlightMode.Linear:
                    Position += Direction * (_settings.Speed * dt);
                    break;
                case FlightMode.Homing:
                    MoveHoming(dt, targetPosition);
                    break;
                case FlightMode.Lobbed:
                    MoveLobbed(dt);
                    break;
                case FlightMode.SeekAndDestroy:
                    MoveSeekAndDestroy(dt, targetPosition);
                    break;
            }
        }

        // Turns toward the target at a limited rate while moving at constant speed. A slow
        // turn rate means the shot sails past a dodging target and curves back for another pass.
        private void MoveHoming(float dt, Vector2? targetPosition)
        {
            if (targetPosition.HasValue)
            {
                Vector2 desired = (targetPosition.Value - Position).normalized;
                float maxRadians = _settings.TurnRate * Mathf.Deg2Rad * dt;
                Direction = ((Vector2)Vector3.RotateTowards(Direction, desired, maxRadians, 0f)).normalized;
                Facing = Direction;
            }

            Position += Direction * (_settings.Speed * dt);
        }

        // Flies a parabola to a fixed landing point. Nothing in between is hit; the shot
        // detonates where it lands, which is what makes mortars feel different from arrows.
        private void MoveLobbed(float dt)
        {
            ArcProgress += dt / _arcDuration;
            if (ArcProgress >= 1f)
            {
                Position = _landingPoint;
                HasLanded = true;
                return;
            }

            Vector2 flat = Vector2.Lerp(_launchPoint, _landingPoint, ArcProgress);
            float height = _settings.ArcHeight * 4f * ArcProgress * (1f - ArcProgress);
            var next = new Vector2(flat.x, flat.y + height);
            Facing = next - Position;
            Position = next;
        }

        // Hovers for a moment, then commits to a target and accelerates into it.
        private void MoveSeekAndDestroy(float dt, Vector2? targetPosition)
        {
            if (!IsDiving)
            {
                if (Age < _settings.LoiterDuration)
                {
                    float bob = Mathf.Sin(Age * Mathf.PI * 2f) * _settings.LoiterBobAmplitude;
                    Position = _loiterOrigin + new Vector2(0f, bob);
                    return;
                }

                IsDiving = true;
            }

            if (targetPosition.HasValue)
            {
                Direction = (targetPosition.Value - Position).normalized;
                Facing = Direction;
            }

            Position += Direction * (_settings.Speed * _settings.DiveSpeedMultiplier * dt);
        }
    }
}
