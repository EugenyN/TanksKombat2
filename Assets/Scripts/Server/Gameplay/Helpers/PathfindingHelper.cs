using System.Runtime.CompilerServices;
using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Mathematics;

namespace TanksKombat2.Server
{
    // Server-only pathfinding helper.
    // Grid model: 4-connectivity (no diagonals), walkable = inside the map and not a wall. Index math and the
    // Directions array are reused from LevelHelper — single source of truth.
    public static class PathfindingHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAdjacentTo(int2 a, int2 b)
        {
            var d = math.abs(a - b);
            return d.x + d.y == 1;
        }
        
        // BFS (unweighted grid => shortest path). outPath is cleared and refilled
        // in 'from -> to' order. Returns false if no path, start/goal invalid, or
        // start==goal with an unreachable config (in which case outPath is left empty).
        public static bool TryFindPath(in LevelDataComponent level, int2 from, int2 to, ref NativeList<int2> outPath,
            bool getUpClose = true, NativeHashSet<int> occupiedCells = default)
        {
            outPath.Clear();

            if (!level.IsWalkable(from))
                return false;

            if (!getUpClose)
            {
                if (!level.Contains(to))
                    return false;

                // Already adjacent: trivial path (matches the previous RemoveAt behaviour,
                // which left a single [from] cell after trimming the goal).
                if (IsAdjacentTo(from, to))
                {
                    outPath.Add(from);
                    return true;
                }
            }
            else
            {
                if (!level.IsWalkable(to))
                    return false;

                if (math.all(from == to))
                {
                    outPath.Add(from);
                    return true;
                }
            }

            var cellCount = level.Size.x * level.Size.y;
            using var cameFromIdx = new NativeHashMap<int, int>(cellCount, Allocator.Temp);
            using var frontier = new NativeQueue<int>(Allocator.Temp);

            var startIdx = level.Index(from);

            cameFromIdx.TryAdd(startIdx, -1);
            frontier.Enqueue(startIdx);

            var goalIdx = -1;

            while (frontier.TryDequeue(out var current))
            {
                var currentCell = level.GridPos(current);
                for (var d = 0; d < 4; d++)
                {
                    var next = currentCell + LevelHelper.Direction(d);
                    if (!level.IsWalkable(next))
                        continue;

                    var nextIdx = level.Index(next);
                    if (cameFromIdx.ContainsKey(nextIdx))
                        continue;
                    
                    if (occupiedCells.IsCreated && occupiedCells.Contains(nextIdx))
                        continue;

                    // Terminal conditions. getUpClose==true  -> reach 'to' itself.
                    // getUpClose==false -> reach any cell adjacent to 'to' (which may
                    // itself be non-walkable; BFS never expands it, so no special case).
                    var reached = getUpClose ? math.all(next == to) : IsAdjacentTo(next, to);
                    if (reached)
                    {
                        cameFromIdx.TryAdd(nextIdx, current);
                        goalIdx = nextIdx;
                        break;
                    }

                    // TryAdd instead of indexer-set: 'using var' makes cameFromIdx readonly,
                    // and the map's this[] setter counts as a member modification (CS1654).
                    // ContainsKey above guarantees the key is new, so TryAdd always succeeds here.
                    cameFromIdx.TryAdd(nextIdx, current);
                    frontier.Enqueue(nextIdx);
                }

                if (goalIdx != -1)
                    break;
            }

            if (goalIdx == -1)
                return false;

            // Reconstruct goal -> start directly into outPath, then reverse it in
            // place (start -> goal) without a temporary list.
            var cursor = goalIdx;
            while (cursor != -1)
            {
                outPath.Add(level.GridPos(cursor));
                if (cursor == startIdx)
                    break;
                cursor = cameFromIdx[cursor];
            }

            for (var i = 0; i < outPath.Length / 2; i++)
            {
                var j = outPath.Length - 1 - i;
                (outPath[i], outPath[j]) = (outPath[j], outPath[i]);
            }

            return outPath.Length > 0;
        }
        // Flood-fill BFS from 'start'. Collects indices of all reachable walkable cells
        // into 'result'. 'visited' is accumulated across calls so previously explored
        // regions are automatically excluded — caller passes the same map repeatedly.
        public static void FindReachableCells(in LevelDataComponent level, int2 start, NativeHashSet<int> visited,
            NativeList<ushort> result)
        {
            result.Clear();

            if (!level.IsWalkable(start))
                return;

            using var frontier = new NativeQueue<int>(Allocator.Temp);

            var startIdx = level.Index(start);
            visited.Add(startIdx);
            frontier.Enqueue(startIdx);
            result.Add((ushort)startIdx);

            while (frontier.TryDequeue(out var current))
            {
                var currentCell = level.GridPos(current);
                for (var d = 0; d < 4; d++)
                {
                    var next = currentCell + LevelHelper.Direction(d);
                    if (!level.IsWalkable(next))
                        continue;

                    var nextIdx = level.Index(next);
                    if (visited.Contains(nextIdx))
                        continue;

                    visited.Add(nextIdx);
                    frontier.Enqueue(nextIdx);
                    result.Add((ushort)nextIdx);
                }
            }
        }
    }
}
