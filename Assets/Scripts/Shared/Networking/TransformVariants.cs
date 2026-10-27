using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // Ghost component variation for LocalTransform replicating 2D-only transform data: position x,y,
    // single rotation angle and scale.
    [GhostComponentVariation(typeof(LocalTransform), "Transform - 2D")]
    [GhostComponent(PrefabType=GhostPrefabType.All, SendTypeOptimization=GhostSendType.AllClients)]
    public struct PositionRotation2d
    {
        // serializes only x,y
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.Clamp, SubType=GhostFieldSubType.Translation2D)]
        public float3 Position;
        // serializes a single signed w component
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.Clamp, SubType=GhostFieldSubType.Rotation2D)]
        public quaternion Rotation;
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.Clamp)]
        public float Scale;
    }
}
