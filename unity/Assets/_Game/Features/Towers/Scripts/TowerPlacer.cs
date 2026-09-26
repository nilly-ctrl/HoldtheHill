using System.Collections.Generic;
using HoldTheHill.Core;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Snaps the mouse to a grid, shows a preview square, and places towers.
    /// Left-click places a tower on an empty cell; right-click removes one.
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("Size of one grid cell in world units.")]
        [SerializeField] private float _cellSize = 1f;

        [Header("Level")]
        [Tooltip("Towers can't be built on this path. Found automatically if left empty.")]
        [SerializeField] private EnemyPath _path;

        [Header("Towers")]
        [Tooltip("Tower prefab to place. Leave empty to use a placeholder square.")]
        [SerializeField] private Tower _towerPrefab;

        [SerializeField] private Color _placeholderColor = new Color(0.3f, 0.8f, 1f);

        [Header("Preview")]
        [SerializeField] private Color _freeCellColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _blockedCellColor = new Color(1f, 0.3f, 0.3f, 0.35f);

        // Which grid cell holds which tower.
        private readonly Dictionary<Vector2Int, Tower> _towers = new Dictionary<Vector2Int, Tower>();

        private Camera _camera;
        private SpriteRenderer _preview;

        private void Awake()
        {
            _camera = Camera.main;
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
            }

            _preview = new GameObject("Placement Preview").AddComponent<SpriteRenderer>();
            _preview.sprite = SpriteUtil.Square;
            _preview.sortingOrder = 100;
            _preview.transform.localScale = Vector3.one * _cellSize;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _camera == null)
            {
                return;
            }

            Vector3 mouseWorld = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2Int cell = WorldToCell(mouseWorld);
            bool occupied = _towers.ContainsKey(cell);
            bool blocked = occupied || IsOnPath(cell);

            _preview.transform.position = CellToWorld(cell);
            _preview.color = blocked ? _blockedCellColor : _freeCellColor;

            if (mouse.leftButton.wasPressedThisFrame && !blocked)
            {
                PlaceTower(cell);
            }
            else if (mouse.rightButton.wasPressedThisFrame && occupied)
            {
                RemoveTower(cell);
            }
        }

        private bool IsOnPath(Vector2Int cell)
        {
            return _path != null && _path.Blocks(CellToWorld(cell), _cellSize);
        }

        private void PlaceTower(Vector2Int cell)
        {
            Tower tower = _towerPrefab != null
                ? Instantiate(_towerPrefab, CellToWorld(cell), Quaternion.identity, transform)
                : CreatePlaceholderTower(cell);

            tower.Cell = cell;
            _towers.Add(cell, tower);
        }

        private void RemoveTower(Vector2Int cell)
        {
            Destroy(_towers[cell].gameObject);
            _towers.Remove(cell);
        }

        private Tower CreatePlaceholderTower(Vector2Int cell)
        {
            var placeholder = new GameObject($"Tower {cell}");
            placeholder.transform.SetParent(transform);
            placeholder.transform.position = CellToWorld(cell);
            placeholder.transform.localScale = Vector3.one * _cellSize * 0.8f;

            var spriteRenderer = placeholder.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteUtil.Square;
            spriteRenderer.color = _placeholderColor;

            return placeholder.AddComponent<Tower>();
        }

        private Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x / _cellSize), Mathf.FloorToInt(world.y / _cellSize));
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * _cellSize, (cell.y + 0.5f) * _cellSize, 0f);
        }
    }
}
