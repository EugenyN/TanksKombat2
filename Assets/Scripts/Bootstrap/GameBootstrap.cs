using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TanksKombat2.Bootstrap
{
    // This is a setup for dealing with a frontend menu, where the user wants control over client and server world creation.
    // We support:
    // - Starting a game into the Frontend scene, allowing the user to choose:
    //      - A 'client hosted' setup.
    //      - A 'connect to existing server via IP' setup.
    // - While starting from any other scene will preserve the existing 'auto-connect' quick-start flow.

    // The preserve attribute is required to make sure the bootstrap is not stripped in il2cpp builds with stripping enabled.
    [UnityEngine.Scripting.Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public const string FrontendSceneName = "Frontend";
        public const string GameSceneName = "GameScene";
        public const string DefaultIPAddress = "127.0.0.1";
        public const ushort DefaultServerPort = 7979;

        // The initialize method is what Entities calls to create the default worlds.
        public override bool Initialize(string defaultWorldName)
        {
            // We check if the loaded scene is "Frontend", which means we should DISABLE auto-connect flows.
            var targetScene = SceneManager.GetActiveScene().name;
            var isFrontend = targetScene == FrontendSceneName;

            Debug.Log(
                $"[GameBootstrap] startupTime: {Time.realtimeSinceStartupAsDouble:0.0}s, isFrontend: {isFrontend}!");

            if (isFrontend)
            {
                CreateFrontendWorld();
            }
            else
            {
                // Use "-port 8000" when running a build from commandline to specify the port to use
                // Will override the default port
                var port = DefaultServerPort;
                if (CommandLineUtils.TryGetCommandLineValueFromKey("port", out string commandPort))
                    port = ParsePortOrDefault(commandPort);

                // Disable auto-connect so we can set RequireConnectionApproval before
                // Listen/Connect (the flag can only be set while the driver is not yet bound).
                AutoConnectPort = 0;

#if UNITY_EDITOR
                // Thin client worlds created inside CreateDefaultClientServerWorlds would never connect:
                // AutoConnectPort is still 0 here and the host client has not connected yet, so
                // ConfigureThinClientWorldSystem.OnCreate (one-shot, no retry) would fail on both of
                // its connect paths. Suppress them; they are created below once AutoConnectPort is set.
                AutomaticThinClientWorldsUtility.NumThinClientsRequested = 0;
#endif
                // Create the appropriate worlds, which we can then load sub-scenes directly into:
                CreateDefaultClientServerWorlds();

                // Enable connection approval and manually start listening on the server.
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
                // Single-world host: the "client" is the host world itself, so no loopback
                // self-connection is made (the fake LocalConnection already represents it).
                // Client-only play creates a normal client world, which must still connect.
                var clientWorld = ServerWorld != null && ServerWorld.IsHost() ? null : ClientWorld;
                StartIpPortClientServer(port, ServerWorld, clientWorld);
#else
                StartIpPortClientServer(port, ServerWorld, ClientWorld);
#endif

#if UNITY_EDITOR
                // Restore the PlayMode Tools preference and create thin client worlds now that the
                // server is already listening and AutoConnectPort is valid —
                // ConfigureThinClientWorldSystem.OnCreate connects them to loopback:port immediately,
                // in time for the server's WaitingForPlayers approval window.
                AutomaticThinClientWorldsUtility.NumThinClientsRequested = null;
                if (RequestedPlayType == PlayType.Client || RequestedPlayType == PlayType.ClientAndServer)
                    AutomaticThinClientWorldsUtility.BootstrapThinClientWorlds();
#endif

#if UNITY_SERVER
                CreateServerMatchConfigOverrideFromCli();
#endif
            }

            return true;
        }

#if UNITY_SERVER
        // Dedicated-server match configuration via CLI (self-host topology skips this:
        // GameManager creates the MatchConfigOverride entity in the server world from
        // the host's GameSettings when starting the server from the menu). Recognized args:
        //   -map <SmallCave|LargeCave|ExtraLargeCave|CustomRandom|Custom>
        //   -custommap <Alien|Jack|Spider>
        //   -theme <Random|Hyrule|Crateria|City|Castle>
        //   -gamemode <Standard|Pro|FireAndIron|QuickDeath>
        //   -botskill <Easy|Normal|Hard>
        //   -botcount <0..7>
        //   -roundstowin <int>
        // Unset / unrecognized values fall back to the ServerSettingsAuthoring baked defaults
        // (no override entity is created, so ServerConfigApplySystem leaves the singleton as baked).
        private static void CreateServerMatchConfigOverrideFromCli()
        {
            bool configured = false;
            var config = new ServerConfigOverride
            {
                MapType = MatchConfigComponent.Default.MapType,
                CustomMap = MatchConfigComponent.Default.CustomMap,
                MapTheme = MatchConfigComponent.Default.MapTheme,
                GameMode = MatchConfigComponent.Default.GameMode,
                BotSkill = MatchConfigComponent.Default.BotSkill,
                BotCount = MatchConfigComponent.Default.BotCount,
                RoundsToWin = MatchConfigComponent.Default.RoundsToWin,
            };

            if (CommandLineUtils.TryGetCommandLineValueFromKey<MapType>("map", out var mapType))
            {
                config.MapType = mapType;
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey<CustomMap>("custommap", out var customMap))
            {
                config.CustomMap = customMap;
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey<MapTheme>("theme", out var mapTheme))
            {
                config.MapTheme = mapTheme;
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey<GameMode>("gamemode", out var gameMode))
            {
                config.GameMode = gameMode;
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey<BotSkill>("botskill", out var botSkill))
            {
                config.BotSkill = botSkill;
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey("botcount", out int botCount))
            {
                config.BotCount = Mathf.Clamp(botCount, 0, ConnectionUtils.MaxSessionPlayers - 1);
                configured = true;
            }

            if (CommandLineUtils.TryGetCommandLineValueFromKey("roundstowin", out int roundsToWin))
            {
                config.RoundsToWin = Mathf.Clamp(roundsToWin, 1, 5);
                configured = true;
            }

            if (!configured)
                return;

            var em = ServerWorld.EntityManager;
            var entity = em.CreateEntity();
            em.AddComponentData(entity, config);
            em.SetName(entity, "MatchConfigOverride (CLI)");

            Debug.Log($"[GameBootstrap] Created match config override from CLI: " +
                      $"map={config.MapType} custommap={config.CustomMap} theme={config.MapTheme} gamemode={config.GameMode} " +
                      $"botskill={config.BotSkill} botcount={config.BotCount} roundstowin={config.RoundsToWin}");
        }
#endif

        // Creates the local frontend world for the menu scene (no networking, LocalSimulation
        // filter). Called when the Frontend scene is active and when returning to the menu
        // (GameManager.QuitGame); AutoConnectPort is cleared so the frontend never auto-connects.
        public static World CreateFrontendWorld()
        {
            // Disable auto-connect so we can set RequireConnectionApproval before
            // Listen/Connect (the flag can only be set while the driver is not yet bound).
            AutoConnectPort = 0;
            
            var world = new World("FrontendWorld", WorldFlags.Game);
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = world;

            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.LocalSimulation);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
            return world;
        }
        
        public static void StartIpPortClientServer(ushort port, World server, World client)
        {
            if (server != null)
            {
                using var serverDriverQuery =
                    server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                ref var serverDriver = ref serverDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;

                serverDriver.RequireConnectionApproval = true;
                serverDriver.Listen(DefaultListenAddress.WithPort(port));
            }
            
            if (client != null && client.IsCreated)
            {
                using var clientDriverQuery =
                    client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());

                ref var clientDriver = ref clientDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                clientDriver.RequireConnectionApproval = true;
                clientDriver.Connect(client.EntityManager, NetworkEndpoint.LoopbackIpv4.WithPort(port));
            }

            // Restore AutoConnectPort so thin clients created later (PlayMode Tools) auto-connect to the same port.
            AutoConnectPort = port;
        }
        
        public static ushort ParsePortOrDefault(string portString)
        {
            if (!ushort.TryParse(portString, out var port))
            {
                Debug.LogWarning($"Unable to parse port, using default port {DefaultServerPort}");
                return DefaultServerPort;
            }

            return port;
        }
    }
}