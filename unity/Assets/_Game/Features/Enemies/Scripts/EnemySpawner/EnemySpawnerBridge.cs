using HoldTheHill.Features.Enemies;
using UnityEngine;

/// <summary>
/// Forwards enemy deaths, and enemies leaving by the end of the path, to
/// <see cref="EnemySpawner"/> so its living-enemy count stays accurate. Put this on the same GameObject as the spawner.
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
        EnemyMover.ReachedEnd += OnEnemyReachedEnd;
    }

    private void OnDisable()
    {
        EnemyHealth.Defeated -= OnEnemyDefeated;
        EnemyMover.ReachedEnd -= OnEnemyReachedEnd;
    }

    // An enemy that walks off the end of the path is gone just as surely as a dead one. Without
    // this the spawner counted every leak as alive forever, so "all enemies cleared" never came
    // true after the first leak.
    private void OnEnemyReachedEnd(GameObject enemy)
    {
        if (_spawner == null || enemy == null)
        {
            return;
        }

        var mover = enemy.GetComponent<EnemyMover>();
        if (mover == null || mover.DespawnsAtEnd)
        {
            _spawner.NotifyEnemyDefeated(enemy);
        }
    }

    private void OnEnemyDefeated(GameObject enemy)
    {
        if (_spawner != null && enemy != null)
        {
            _spawner.NotifyEnemyDefeated(enemy);
        }
    }
}
