using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Attaches the player's save file for this scene and writes it when the game closes.
    /// Without one in the scene nothing is read from or written to disk.
    /// </summary>
    /// <remarks>
    /// Runs before everything else so other scripts can read saved values in their own Awake.
    /// </remarks>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Save Host")]
    public class GrayboxSaveHost : MonoBehaviour
    {
        private void Awake()
        {
            if (!GrayboxSave.IsOpen)
            {
                GrayboxSave.Open();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) GrayboxSave.SaveIfDirty();
        }

        private void OnApplicationQuit() => GrayboxSave.SaveIfDirty();

        private void OnDestroy() => GrayboxSave.SaveIfDirty();
    }
}
