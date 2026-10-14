using UnityEngine;
using UnityEngine.Tilemaps;

namespace HoldTheHill.Features.Levels
{
    /// <summary>
    /// A ground tile that picks one of several sprites from the cell's position, so a large
    /// painted area does not visibly repeat and looks the same every time the scene loads.
    /// With <see cref="odd"/> filled in, cells alternate between the two lists like a
    /// chessboard (the kitchen's two tile colours).
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Ground Variant Tile")]
    public class GroundVariantTile : TileBase
    {
        [Tooltip("Sprites for every cell, or for the cells where x + y is even when Odd is filled in.")]
        public Sprite[] even = new Sprite[0];

        [Tooltip("Sprites for the cells where x + y is odd. Leave empty for one kind of ground.")]
        public Sprite[] odd = new Sprite[0];

        [Tooltip("How many sprites at the start of each list are the plain ones.")]
        [Min(1)] public int plainCount = 2;

        [Tooltip("Share of cells that use a plain sprite. The rest get one with a detail on it.")]
        [Range(0f, 1f)] public float plainShare = 0.5f;

        [Tooltip("Flip half the cells left to right, for twice the variety. Only for ground with no pattern or lighting.")]
        public bool mirror;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            bool useOdd = odd != null && odd.Length > 0 && ((position.x + position.y) & 1) != 0;
            Sprite[] sprites = useOdd ? odd : even;
            tileData.colliderType = Tile.ColliderType.None;
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.flags = TileFlags.LockTransform;
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            uint hash = Hash(position.x, position.y);
            int plain = Mathf.Clamp(plainCount, 1, sprites.Length);
            bool usePlain = plain == sprites.Length || (hash & 0xFFFF) / 65536f < plainShare;
            uint pick = hash >> 16;
            int index = usePlain ? (int)(pick % (uint)plain) : plain + (int)(pick % (uint)(sprites.Length - plain));
            tileData.sprite = sprites[index];
            if (mirror && (Hash(position.y + 7919, position.x) & 1) != 0)
            {
                tileData.transform = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
            }
        }

        private static uint Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                h = (h ^ (h >> 15)) * 0x2C1B3C6Du;
                h = (h ^ (h >> 12)) * 0x297A2D39u;
                return h ^ (h >> 15);
            }
        }
    }
}
