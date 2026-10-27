using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TanksKombat2.Bootstrap;
using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TanksKombat2.Client
{
    // Client-side match flow orchestrator for the frontend/in game menu: hosts/joins games via Relay
    // sessions or Direct IP, owns the ConnectionManager, writes match config overrides into the
    // server world, disposes worlds and drives scene transitions.
    [AutoStaticsCleanup]
    public partial class GameManager
    {
        public static GameManager Instance { get; private set; } = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new GameManager();

        public ConnectionManager ConnectionManager { get; } = new();

        // Hosts a Relay/Session game (client + server in one process): awaits the session
        // creation result (the player stays in the menu with the MessageBox while connecting),
        // and only loads the game scene after the session is established. On cancel/failure the
        // partially-created worlds and any in-flight session are cleaned up and the exception
        // is rethrown.
        public async Task StartGameRelay(string portText, string sessionIdText,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
                throw new Exception($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");

            try
            {
                await ConnectionManager.StartSessionsClientServer(portText, sessionIdText, progress, ct);
            }
            catch
            {
                await ConnectionManager.Disconnect();
                if (ClientServerBootstrap.ServerWorld != null && ClientServerBootstrap.ServerWorld.IsCreated)
                    ClientServerBootstrap.ServerWorld.Dispose();
                if (ClientServerBootstrap.ClientWorld != null && ClientServerBootstrap.ClientWorld.IsCreated)
                    ClientServerBootstrap.ClientWorld.Dispose();
                throw;
            }

            CreateMatchConfigOverride(ClientServerBootstrap.ServerWorld);
            progress?.Report("Loading game scene...");
            LoadScenes(ClientServerBootstrap.ServerWorld);
        }

        // Hosts a Direct-IP game (client + server in one process, loopback connection). Synchronous
        // (no awaitable connection wait — loopback connects instantly). On failure the partially-
        // created worlds are disposed and the exception is rethrown.
        public void StartGameDirectIP(string portText, IProgress<string> progress = null)
        {
            progress?.Report("Creating worlds...");
            World server = null;
            World client = null;
            try
            {
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
                var hostWorldMode = NetCodeConfig.Global.HostWorldModeSelection;
                if (hostWorldMode == NetCodeConfig.HostWorldMode.SingleWorld)
                {
                    server = ClientServerBootstrap.CreateSingleWorldHost("HostWorld");
                }
                else
#endif
                {
                    server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                    client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                }

                CreateMatchConfigOverride(server);
                ConnectionManager.StartIpPortClientServer(portText, server, client, progress);
            }
            catch
            {
                client?.Dispose();
                server?.Dispose();
                throw;
            }

            progress?.Report("Loading game scene...");
            LoadScenes(server);
        }

        // Joins a Direct-IP server as a pure client. The client world is created inside the try
        // block so that any failure — including cancellation of the one-frame wait below —
        // disposes the partially-created world instead of leaking it.
        public async Task JoinGameDirectIp(string addressText, string portText,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            progress?.Report("Creating client world...");
            World client = null;
            try
            {
                client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

                // Let the fresh client world run one frame (systems OnCreate, driver entity setup)
                // before the connection is started.
                await Awaitable.NextFrameAsync(ct);

                await ConnectionManager.ConnectToIpPortServer(addressText, portText, client.EntityManager, progress, ct);
            }
            catch
            {
                client?.Dispose();
                throw;
            }

            progress?.Report("Loading game scene...");
            LoadScenes(client);
        }

        // Joins a Relay session as a pure client. On cancel/failure the partial session and any
        // client world the handler created are cleaned up and the exception is rethrown.
        public async Task JoinGameRelay(string sessionIdText,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            try
            {
                await ConnectionManager.ConnectToSessionsServer(sessionIdText, progress, ct);
            }
            catch
            {
                // Tear down any partial session and the client world the handler may have created.
                await ConnectionManager.Disconnect();
                if (ClientServerBootstrap.ClientWorld != null && ClientServerBootstrap.ClientWorld.IsCreated)
                    ClientServerBootstrap.ClientWorld.Dispose();
                throw;
            }

            progress?.Report("Loading game scene...");
            LoadScenes(ClientServerBootstrap.ClientWorld);
        }

        // Returns to the main menu: tears down all client/server worlds, recreates the frontend
        // world and loads the Frontend scene.
        public void QuitGame()
        {
            _ = ConnectionManager.Disconnect();

            // Session destroys the server world but server destruction will also be covered here when not using sessions.
            // Client world will always need to be destroyed
            var clientServerWorlds = new List<World>();
            foreach (var world in World.All)
            {
                if (world.IsClient() || world.IsServer())
                    clientServerWorlds.Add(world);
            }

            foreach (var world in clientServerWorlds)
            {
                Debug.Log($"Disposing World {world.Name}");
                world.Dispose();
            }

            GameBootstrap.CreateFrontendWorld();

            SceneManager.LoadScene(GameBootstrap.FrontendSceneName, LoadSceneMode.Single);
        }

        private static void CreateMatchConfigOverride(World serverWorld)
        {
            var settings = GameSettings.Instance;
            var em = serverWorld.EntityManager;
            em.AddComponentData(em.CreateEntity(), new ServerConfigOverride
            {
                MapType = settings.MapType,
                CustomMap = settings.CustomMap,
                MapTheme = settings.MapTheme,
                GameMode = settings.GameMode,
                BotSkill = settings.BotSkill,
                BotCount = settings.BotCount,
                RoundsToWin = settings.RoundsToWin,
            });
        }

        private void LoadScenes(World world)
        {
            // Destroy the local simulation world to avoid the game scene to be loaded into it
            // This prevents rendering (rendering from multiple world with presentation is not greatly supported)
            // and other issues.
            DestroyFrontendWorld();

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = world;

            SceneManager.LoadScene(GameBootstrap.GameSceneName);
        }

        private void DestroyFrontendWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }
    }
}
