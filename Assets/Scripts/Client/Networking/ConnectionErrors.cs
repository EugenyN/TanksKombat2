using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Maps the connection state tracked by ClientConnectionStateSystem (last event for the
    // client's server connection) to a user-facing rejection message for the async connect-polls.
    public static class ConnectionErrors
    {
        public static string GetMessage(EntityManager entityManager, Entity connectionEntity)
        {
            if (!ClientConnectionStateSystem.TryGet(entityManager, connectionEntity, out var info) ||
                info.State != ConnectionState.State.Disconnected)
            {
                return "Server rejected the connection: match in progress or server full.";
            }
            
            switch (info.Reason)
            {
                case NetworkStreamDisconnectReason.InvalidRpc:
                case NetworkStreamDisconnectReason.BadProtocolVersion:
                    return "Client had invalid protocol version!";

                case NetworkStreamDisconnectReason.Timeout:
                    return "Connection timeout!";

                case NetworkStreamDisconnectReason.MaxConnectionAttempts:
                    return "Connection exceeded max connection attempts!";

                case NetworkStreamDisconnectReason.HandshakeTimeout:
                case NetworkStreamDisconnectReason.ApprovalTimeout:
                    return "Connection failed: handshake or approval timeout!";

                case NetworkStreamDisconnectReason.ApprovalFailure:
                    return "Server rejected the connection: match in progress or server full!";
                
                case NetworkStreamDisconnectReason.ConnectionClose:
                case NetworkStreamDisconnectReason.ClosedByRemote:
                    return "Server rejected the connection!";
                
                case NetworkStreamDisconnectReason.ProtocolError:
                    return "Client had a low-level transport error!";
                
                case NetworkStreamDisconnectReason.AuthenticationFailure:
                    return "Client could not be authenticated!";
                
                default:
                    return $"Server rejected the connection with error {(int)info.Reason}!";
            }
        }
    }
}
