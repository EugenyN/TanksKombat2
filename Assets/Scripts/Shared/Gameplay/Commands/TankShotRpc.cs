using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C event RPC: a tank fired (possibly with empty ammo); drives the client muzzle flash/sound effect.
    public struct TankShotRpc : IRpcCommand
    {
        public int OwnerNetworkId;
        public bool EmptyAmmo;
    }
}