using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Shared
{
    // Shared grid-collision helper (server authority + client prediction).
    // Per-frame it rebuilds a cell->CollisionInfo multimap over dynamic colliders;
    public readonly partial struct CollisionWorld
    {
        public readonly struct CollisionInfo
        {
            public readonly Entity Entity;
            public readonly CollisionFlags CollidesWith;
            public readonly bool IsTrigger;
            public readonly int OwnerNetworkId;

            public CollisionInfo(Entity entity, ColliderComponent collider, int ownerNetworkId)
            {
                Entity = entity;
                CollidesWith = collider.CollidesWith;
                IsTrigger = collider.IsTrigger;
                OwnerNetworkId = ownerNetworkId;
            }
        }

        public readonly LevelDataComponent Level;
        [ReadOnly] public readonly NativeParallelMultiHashMap<int2, CollisionInfo>.ReadOnly CollisionMap;

        private CollisionWorld(in LevelDataComponent level, 
            NativeParallelMultiHashMap<int2, CollisionInfo>.ReadOnly collisionMap)
        {
            Level = level;
            CollisionMap = collisionMap;
        }
        
        public static (CollisionWorld World, BuildCollisionMapJob Job) Create(
            ref SystemState state, EntityQuery collidersQuery, in LevelDataComponent level, 
            in ComponentLookup<GhostOwner> ghostOwnerLookup)
        {
            var collisionMap = new NativeParallelMultiHashMap<int2, CollisionInfo>(
                collidersQuery.CalculateEntityCount(), state.WorldUpdateAllocator);

            var buildJob = new BuildCollisionMapJob
            {
                GhostOwnerLookup = ghostOwnerLookup,
                CollisionMap = collisionMap
            };

            return (new CollisionWorld(level, collisionMap.AsReadOnly()), buildJob);
        }
        
        [BurstCompile]
        public partial struct BuildCollisionMapJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<GhostOwner> GhostOwnerLookup;

            public NativeParallelMultiHashMap<int2, CollisionInfo> CollisionMap;

            private void Execute(Entity entity, in LocalTransform transform, in ColliderComponent collider)
            {
                var ownerNetworkId = GhostOwnerLookup.HasComponent(entity)
                    ? GhostOwnerLookup[entity].NetworkId
                    : -1;

                CollisionMap.Add(LevelHelper.WorldToGrid(transform.Position),
                    new CollisionInfo(entity, collider, ownerNetworkId));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryHitCell(Entity entity, int2 cell, CollisionFlags flags, out Entity target, out bool isTrigger)
        {
            return TryHitCell(entity, cell, flags, -1, out target, out isTrigger);
        }

        // Cell-by-cell walk. Orthogonal (axis-aligned) casts only: both axes step simultaneously,
        // so a diagonal delta never visits toCell and can read past it. Callers must guarantee
        // fromCell/toCell share one axis (tanks move on the grid, bullets travel along one axis).
        public bool GridCast(Entity entity, int2 fromCell, int2 toCell, CollisionFlags flags, int ignoreNetworkId,
            out Entity target, out bool isTrigger, out int2 hitCell)
        {
            target = Entity.Null;
            isTrigger = false;
            hitCell = fromCell;

            var delta = toCell - fromCell;
            var step = math.sign(delta);
            var count = math.abs(delta.x) + math.abs(delta.y);

            var cell = fromCell;
            for (var i = 0; i <= count; i++)
            {
                if (TryHitCell(entity, cell, flags, ignoreNetworkId, out target, out isTrigger))
                {
                    hitCell = cell;
                    return true;
                }

                cell += step;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryHitCell(Entity entity, int2 cell, CollisionFlags flags, int ignoreNetworkId,
            out Entity target, out bool isTrigger)
        {
            target = Entity.Null;
            isTrigger = false;

            if (!Level.Contains(cell) || Level.GetTile(cell).IsWall())
                return true;

            if (!CollisionMap.TryGetFirstValue(cell, out var info, out var iterator))
                return false;

            do
            {
                // NOTE: with multiple hits in one cell the winner is the first value in the
                // multimap, whose order depends on parallel insertion -> non-deterministic and
                // may differ between server and predicted client (see GridCast callers).
                // Safe only while gameplay ignores which target was picked.
                if (info.Entity == entity)
                    continue;

                if ((info.CollidesWith & flags) == 0)
                    continue;

                if (ignoreNetworkId != -1 && info.OwnerNetworkId == ignoreNetworkId)
                    continue;

                target = info.Entity;
                isTrigger = info.IsTrigger;
                return true;
            } while (CollisionMap.TryGetNextValue(out info, ref iterator));

            return false;
        }
    }
}