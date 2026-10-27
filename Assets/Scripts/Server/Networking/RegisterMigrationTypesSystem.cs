using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.HostMigration;

namespace TanksKombat2.Server
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(RpcSystem))]
    public partial struct RegisterMigrationTypesSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var migrationTypesEntity = state.EntityManager.CreateSingletonBuffer<NonGhostMigrationComponents>();
            var migrationTypesBuffer =
                state.EntityManager.GetBuffer<NonGhostMigrationComponents>(migrationTypesEntity);

            migrationTypesBuffer.Add(new NonGhostMigrationComponents
                { ComponentToMigrate = ComponentType.ReadOnly<ServerSettingsComponent>() });
            migrationTypesBuffer.Add(new NonGhostMigrationComponents
                { ComponentToMigrate = ComponentType.ReadOnly<ServerMapSizesComponent>() });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}