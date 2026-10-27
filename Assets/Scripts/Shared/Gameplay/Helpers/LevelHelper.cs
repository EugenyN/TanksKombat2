using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    public static class LevelHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Direction(int direction)
        {
            // 0=up, 1=right, 2=down, 3=left
            return (direction & 3) switch { 
                0 => new int2(0, 1), 1 => new int2(1, 0), 2 => new int2(0, -1), _ => new int2(-1, 0)
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWall(this MapTile tile)
        {
            return tile == MapTile.Wall1 ||  tile == MapTile.Wall2 || tile == MapTile.Wall3;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBackground(this MapTile tile)
        {
            return tile == MapTile.Background1 || tile == MapTile.Background2 || tile == MapTile.Background3;
        }
        
        // Unit "forward" vector in world space. 2D convention: sprites face local +Y.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Forward(in quaternion rotation)
        {
            return math.mul(rotation, new float3(0f, 1f, 0f)).xy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDirection(in quaternion rotation)
        {
            float2 dir = Forward(rotation);

            if (math.abs(dir.x) > math.abs(dir.y))
                return dir.x > 0f ? 1 : 3; // right : left

            return dir.y > 0f ? 0 : 2; // up : down
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MapTile GetTile(this in LevelDataComponent levelData, int2 gridPos)
        {
            return levelData.LevelData[gridPos.y * levelData.Size.x + gridPos.x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 WorldToGrid(float3 worldPos)
        {
            return new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GridToWorld(int2 gridPos)
        {
            return new float3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 GridPos(this in LevelDataComponent level, int index)
        {
            return GridPos(level.Size, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 GridPos(int2 size, int index)
        {
            return new int2(index % size.x, index / size.x);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(this in LevelDataComponent level, int2 gridPos)
        {
            return Index(level.Size, gridPos);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(int2 size, int2 gridPos)
        {
            return gridPos.y * size.x + gridPos.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(this in LevelDataComponent level, float3 worldPos)
        {
            return Index(level.Size, worldPos);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(int2 size, float3 worldPos)
        {
            return Index(size, WorldToGrid(worldPos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this in LevelDataComponent level, int2 gridPos)
        {
            return Contains(level.Size, gridPos);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(int2 size, in int2 gridPos)
        {
            return gridPos.x >= 0 && gridPos.x <= size.x - 1 && gridPos.y >= 0 && gridPos.y <= size.y - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 WrapGridPos(this in LevelDataComponent level, int2 gridPos)
        {
            return WrapGridPos(level.Size, gridPos);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 WrapGridPos(int2 size, int2 gridPos)
        {
            return new int2((gridPos.x % size.x + size.x) % size.x, (gridPos.y % size.y + size.y) % size.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWalkable(this in LevelDataComponent level, int2 gridPos)
        {
            return level.Contains(gridPos) && !level.GetTile(gridPos).IsWall();
        }
    }
}