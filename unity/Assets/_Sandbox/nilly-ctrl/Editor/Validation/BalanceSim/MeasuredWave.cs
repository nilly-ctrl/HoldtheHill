namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>One wave as measured by the in-scene harness, for comparison with the model.</summary>
    public sealed class MeasuredWave
    {
        public MeasuredWave(int number, int spawned, int killed, int leaked)
        {
            Number = number;
            Spawned = spawned;
            Killed = killed;
            Leaked = leaked;
        }

        public int Number { get; }

        public int Spawned { get; }

        public int Killed { get; }

        public int Leaked { get; }
    }
}
