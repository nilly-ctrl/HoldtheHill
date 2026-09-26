using HoldTheHill.Features.Enemies;
using UnityEngine;

/// <summary>
/// Forwards enemy deaths to <see cref="EnemySpawner"/> so its living-enemy count stays
/// accurate. Put this on the same GameObject as the spawner.
/// </summary>
/// <remarks>
/// This exists because of an assembly boundary, not because the indirection is desirable.
/// <c>EnemyHealth</c> lives in <c>HoldTheHill.Runtime</c> (the asmdef at <c>_Game/</c>), while
/// <c>EnemySpawner</c> sits in <c>Assets/Scripts/</c> with no asmdef, so it compiles into
/// <c>Assembly-CSharp</c>. Unity builds asmdef assemblies first, so the dependency can only
/// run one way: this file can see both, but <c>EnemyHealth</c> can never call the spawner
/// directly.
///
/// Move <c>EnemySpawner</c> into <c>_Game/Features/Enemies/</c> and this file can be deleted,
/// with the spawner subscribing to <see cref="EnemyHealth.Defeated"/> itself.
/// </remarks>
[AddComponentMenu("Hold the Hill/Enemies/Enemy Spawner Bridge")]
[RequireComponent(typeof(EnemySpawner))]
public class EnemySpawnerBridge : MonoBehaviour
{
    private EnemySpawner _spawner;

    private void Awake()
    {
        _spawner = GetComponent<EnemySpawner>();
    }

    private void OnEnable()
    {
        EnemyHealth.Defeated += OnEnemyDefeated;
    }

    private void OnDisable()
    {
        EnemyHealth.Defeated -= OnEnemyDefeated;
    }

    private void OnEnemyDefeated(GameObject enemy)
    {
        if (_spawner != null && enemy != null)
        {
            _spawner.NotifyEnemyDefeated(enemy);
        }
    }
}
