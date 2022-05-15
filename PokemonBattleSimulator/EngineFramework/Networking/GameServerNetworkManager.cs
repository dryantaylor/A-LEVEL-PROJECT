using System.Collections.Generic;
using System.Text;

namespace PokemonBattleSimulator.EngineFramework.Networking
{
    public class GameServerNetworkManager: ServerNetworkManager
    {
        public GameServerNetworkManager(string hostString, int port, byte[] thisUname, byte[] password, byte[] opponentUname) : base(hostString, port,thisUname)
        {
            if (IsSocketOpen)
            {
                List<byte> msgBytes = new List<byte>();
                msgBytes.AddRange(password);
                msgBytes.AddRange(thisUname);
                SendMessage(msgBytes.ToArray());

                var (length, response) = ReceiveMessage(1000);
                if (length > 0 && Encoding.UTF8.GetString(response).StartsWith("A"))
                {
                    return;
                }
                
                {
                    IsSocketOpen = false;
                    CaughtFailure.errorResponse = Encoding.UTF8.GetString(response[1..(int)length]);
                }
            }

        }
    }
}
