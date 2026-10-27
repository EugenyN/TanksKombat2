using TanksKombat2.Shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace TanksKombat2.Client
{
    // Client presentation: builds the full-tile background mesh (one quad per level cell)
    // from LevelData and assigns it (mesh id + render bounds) to the TileMapBackground entity.
    // Runs once per level load; OnUpdate stays managed (mesh building + EntitiesGraphicsSystem).
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct TileMapBackgroundBuildSystem : ISystem
    {
        // UV inset (in atlas pixels) applied to every tile rect so sampling the atlas
        // does not bleed in neighboring tiles at the edges.
        private const float UvPaddingPixels = 0.001f;
        
        private BatchMeshID _backgroundMeshId;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileMapBackground>();
            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<LevelDataComponent>();

            state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<LoadLevelRpc>(),
                ComponentType.ReadOnly<ReceiveRpcCommandRequest>()));
        }

        public void OnUpdate(ref SystemState state)
        {
            var entitiesGraphics = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

            var levelCfg = SystemAPI.GetSingleton<MatchConfigComponent>();
            var levelData = SystemAPI.GetSingleton<LevelDataComponent>();
            var clientSettings = SystemAPI.GetSingleton<ClientSettingsComponent>();
            var tileSetConfig = clientSettings.TileSetConfig.Value;
            var mapSize = levelData.Size;
            int themeRow = TileSetConfig.GetThemeRow(levelCfg.MapTheme);

            if (_backgroundMeshId != BatchMeshID.Null)
            {
                var registeredMesh = entitiesGraphics.GetMesh(_backgroundMeshId);
                Object.Destroy(registeredMesh);
                entitiesGraphics.UnregisterMesh(_backgroundMeshId);
            }

            var mesh = BuildBackgroundMesh(tileSetConfig, in levelData, themeRow);
            _backgroundMeshId = entitiesGraphics.RegisterMesh(mesh);

            var background = SystemAPI.GetSingletonEntity<TileMapBackground>();
            var mmi = state.EntityManager.GetComponentData<MaterialMeshInfo>(background);
            mmi.Mesh = (int)_backgroundMeshId.value;
            state.EntityManager.SetComponentData(background, mmi);

            state.EntityManager.SetComponentData(background, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new float3(mapSize.x * 0.5f, mapSize.y * 0.5f, 0f),
                    Extents = new float3(mapSize.x * 0.5f, mapSize.y * 0.5f, 1f)
                }
            });

            state.EntityManager.RemoveComponent<DisableRendering>(background);
        }

        private static Mesh BuildBackgroundMesh(TileSetConfig tileSetConfig,
            in LevelDataComponent levelData, int themeRow)
        {
            int sx = levelData.Size.x;
            int sy = levelData.Size.y;
            int cells = sx * sy;

            var vertices = new Vector3[cells * 4];
            var uvs = new Vector2[cells * 4];
            var triangles = new int[cells * 6];
            
            float invW = 1f / (tileSetConfig.Columns * tileSetConfig.TilePixelSize);
            float invH = 1f / (tileSetConfig.Rows * tileSetConfig.TilePixelSize);

            int vRowBottom = tileSetConfig.Rows - 1 - themeRow;

            for (int y = 0; y < sy; y++)
            {
                for (int x = 0; x < sx; x++)
                {
                    int col = (int)levelData.LevelData[y * sx + x];
                    if (col > 2) col = 0;

                    float u0 = (col * tileSetConfig.TilePixelSize + UvPaddingPixels) * invW;
                    float u1 = ((col + 1) * tileSetConfig.TilePixelSize - UvPaddingPixels) * invW;
                    float v0 = (vRowBottom * tileSetConfig.TilePixelSize + UvPaddingPixels) * invH;
                    float v1 = ((vRowBottom + 1) * tileSetConfig.TilePixelSize - UvPaddingPixels) * invH;

                    int v = (y * sx + x) * 4;
                    vertices[v + 0] = new Vector3(x, y, 0);
                    vertices[v + 1] = new Vector3(x + 1, y, 0);
                    vertices[v + 2] = new Vector3(x, y + 1, 0);
                    vertices[v + 3] = new Vector3(x + 1, y + 1, 0);

                    uvs[v + 0] = new Vector2(u0, v0);
                    uvs[v + 1] = new Vector2(u1, v0);
                    uvs[v + 2] = new Vector2(u0, v1);
                    uvs[v + 3] = new Vector2(u1, v1);

                    int t = (y * sx + x) * 6;
                    triangles[t + 0] = v + 0;
                    triangles[t + 1] = v + 2;
                    triangles[t + 2] = v + 1;
                    triangles[t + 3] = v + 1;
                    triangles[t + 4] = v + 2;
                    triangles[t + 5] = v + 3;
                }
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = "BackgroundTiles" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, false);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}