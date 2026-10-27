using System.Runtime.CompilerServices;
using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace TanksKombat2.Server
{
    // Server-only helper around LevelReachableCellsComponent: reachability checks against the
    // pre-computed largest walkable region, random spawn-point picking that avoids occupied
    // cells, and Burst-compiled flood-fill that computes the region itself (ComputeReachableCells).
    public static class ReachableCellsHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReachable(this ref LevelReachableCellsComponent reachableCells, in int2 size,
            in int2 gridPos)
        {
            return reachableCells.Cells.Contains((ushort)LevelHelper.Index(size, gridPos));
        }

        public static bool TryFindSpawnPosition(this in LevelReachableCellsComponent reachableCells, in int2 size,
            ref Random rand, NativeArray<LocalTransform> avoidTransforms, out int2 resultPosition)
        {
            if (reachableCells.Cells.Length == 0)
            {
                resultPosition = default;
                return false;
            }

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var index = rand.NextInt(reachableCells.Cells.Length);
                resultPosition = LevelHelper.GridPos(size, reachableCells.Cells[index]);

                if (IsEntityOnPosition(avoidTransforms, resultPosition))
                    continue;

                return true;
            }

            resultPosition = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEntityOnPosition(NativeArray<LocalTransform> avoidTransforms, int2 position)
        {
            for (var i = 0; i < avoidTransforms.Length; i++)
            {
                if (math.all(LevelHelper.WorldToGrid(avoidTransforms[i].Position) == position))
                    return true;
            }

            return false;
        }
        
        private static bool TryFindNearestWalkableToCenter(in LevelDataComponent level, NativeHashSet<int> visited,
            out int2 result)
        {
            var center = level.Size / 2;
            var bestDist = int.MaxValue;
            result = default;
            var found = false;

            for (var i = 0; i < level.LevelData.Length; i++)
            {
                if (level.LevelData[i].IsWall())
                    continue;

                if (visited.Contains(i))
                    continue;

                var cell = level.GridPos(i);
                var dist = math.abs(cell.x - center.x) + math.abs(cell.y - center.y);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    result = cell;
                    found = true;
                }
            }

            return found;
        }

        // Runs the whole multi-region flood-fill inside a single Burst job
        public static FixedList4096Bytes<ushort> ComputeReachableCells(in LevelDataComponent level)
        {
            using var result = new NativeList<ushort>(Allocator.TempJob);
            var job = new FindReachableRegionsJob { Level = level, Result = result, MinRegionCells = 10 };
            job.RunByRef();

            var cells = new FixedList4096Bytes<ushort>();
            cells.AddRange(job.Result);
            return cells;
        }


        // Flood-fills every disconnected walkable region and returns the largest one found.
        // Regions smaller than MinRegionCells are ignored; if none qualifies, Result stays
        // empty and callers (e.g. TryFindSpawnPosition) report failure.
        [BurstCompile]
        private struct FindReachableRegionsJob : IJob
        {
            public LevelDataComponent Level;
            public NativeList<ushort> Result;
            public int MinRegionCells;

            public void Execute()
            {
                var visited = new NativeHashSet<int>(Level.LevelData.Length, Allocator.Temp);
                var region = new NativeList<ushort>(Allocator.Temp);
                var best = new NativeList<ushort>(Allocator.Temp);

                while (true)
                {
                    if (!TryFindNearestWalkableToCenter(in Level, visited, out var start))
                        break;

                    PathfindingHelper.FindReachableCells(Level, start, visited, region);

                    // Keep the largest region seen so far. FindReachableCells clears 'region'
                    // on entry, so the swapped-out buffer is reused without extra allocation.
                    if (region.Length > best.Length)
                        (best, region) = (region, best);
                }

                if (best.Length >= MinRegionCells)
                    Result.AddRange(best);

                visited.Dispose();
                region.Dispose();
                best.Dispose();
            }
        }
    }
}