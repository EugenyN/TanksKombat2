using Unity.Collections;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // Predicted per-tank command input (move/shoot/block events); sent from owner, replayed on server.
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct TankPlayerInput : IInputComponentData
    {
        public InputEvent MoveUp;
        public InputEvent MoveRight;
        public InputEvent MoveDown;
        public InputEvent MoveLeft;
        public InputEvent Shoot;
        public InputEvent Block;

        public bool IsMove => MoveUp.IsSet || MoveRight.IsSet || MoveDown.IsSet || MoveLeft.IsSet;

        public int GetDirection()
        {
            if (MoveUp.IsSet) return 0;
            if (MoveRight.IsSet) return 1;
            if (MoveDown.IsSet) return 2;
            if (MoveLeft.IsSet) return 3;
            return -1;
        }

        public void SetMove(int dir)
        {
            switch (dir)
            {
                case 0: MoveUp.Set(); break;
                case 1: MoveRight.Set(); break;
                case 2: MoveDown.Set(); break;
                case 3: MoveLeft.Set(); break;
            }
        }

        public void SetDirection(int dir)
        {
            Block.Set();
            SetMove(dir);
        }

        public void SetShoot()
        {
            Shoot.Set();
        }

        public FixedString512Bytes ToFixedString() =>
            $"Direction: {GetDirection()} Shoot: {Shoot.Count}  Block: {Block.Count}";
    }
}