#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Frame times over the last N frames (a ring buffer, no allocation after construction).
    /// Times are in seconds; the properties give milliseconds.
    /// </summary>
    public sealed class DevFrameStats
    {
        private readonly float[] _samples;
        private int _next;
        private int _count;
        private double _sum;

        public DevFrameStats(int capacity = 120)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _samples = new float[capacity];
        }

        public int Capacity => _samples.Length;

        /// <summary>How many frames are in the window so far, up to <see cref="Capacity"/>.</summary>
        public int Count => _count;

        public void Add(float seconds)
        {
            if (seconds < 0f) seconds = 0f;

            if (_count == _samples.Length) _sum -= _samples[_next];
            else _count++;

            _samples[_next] = seconds;
            _sum += seconds;
            _next = (_next + 1) % _samples.Length;
        }

        public void Clear()
        {
            _next = 0;
            _count = 0;
            _sum = 0;
        }

        public float AverageMs => _count == 0 ? 0f : (float)(_sum / _count * 1000.0);

        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _count; i++)
                {
                    if (_samples[i] > worst) worst = _samples[i];
                }

                return worst * 1000f;
            }
        }

        /// <summary>Frames per second implied by the average frame time.</summary>
        public float Fps
        {
            get
            {
                float ms = AverageMs;
                return ms > 0f ? 1000f / ms : 0f;
            }
        }
    }
}
#endif
