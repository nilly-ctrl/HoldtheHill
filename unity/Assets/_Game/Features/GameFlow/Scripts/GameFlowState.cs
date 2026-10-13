namespace HoldTheHill.Features.GameFlow
{
    /// <summary>Which screen the game is on. Time runs only in <see cref="Playing"/>.</summary>
    public enum GameFlowState
    {
        Title,
        Home,
        Playing,
        Paused,
        RunEnd,
    }
}
