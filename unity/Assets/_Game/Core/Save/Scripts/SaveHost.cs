using UnityEngine;

namespace HoldTheHill.Core.Save
{
    /// <summary>
    /// Attaches the player's save file for this scene and writes it when the game closes.
    /// Without one in the scene nothing is read from or written to disk.
    /// </summary>
    /// <remarks>
    /// Runs before everything else so other scripts can read saved values in their own Awake.
    /// </remarks>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Hold the Hill/Graybox Save Host")]
    public class SaveHost : MonoBehaviour
    {
        private void Awake()
        {
            if (!SaveStore.IsOpen)
            {
                SaveStore.Open();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveStore.SaveIfDirty();
        }

        private void OnApplicationQuit() => SaveStore.SaveIfDirty();

        private void OnDestroy() => SaveStore.SaveIfDirty();
    }
}
