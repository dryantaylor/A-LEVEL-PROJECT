using System;
using System.Collections.Generic;
using System.Text;

namespace PokemonBattleSimulator.EngineFramework.Networking
{
    public class MainServerNetworkManager : ServerNetworkManager
    {
        public List<byte[]> Users = new List<byte[]>();
        public List<byte[]> BattleRequestsRecieved = new List<byte[]>();
        public List<byte[]> BattleRequestsSent = new List<byte[]>();
        public MainServerNetworkManager(string hostString, int port, byte[] checkSums, byte[] uname): base(hostString,port,uname)
        {
            try {
                SendMessage(checkSums);
                var (responseLength, response) = ReceiveMessage(1000);
                if (responseLength == null)
                {
                    return; //error handling here
                }
                if (!Encoding.UTF8.GetString(response).StartsWith("A"))
                {
                    CaughtFailure.errorResponse = Encoding.UTF8.GetString(response,1,(int)responseLength-1);
                    Close();
                }
                else
                {
                    //checksums accepted so send uname
                    SendMessage(uname);
                    (responseLength, response) = ReceiveMessage(-1);
                    if (!Encoding.UTF8.GetString(response).StartsWith("A"))
                    {
                        CaughtFailure.errorResponse = Encoding.UTF8.GetString(response, 1, (int)responseLength - 1);
                        Close();
                    }
                    else
                    {
                        //this gets all the users connected at the start
                        //if it returns false which means it hasnt been able to get them all7
                        //it causes a fail in the start of main server
                        if (!GetInitialConnectedUsers(Encoding.UTF8.GetString(response)[1..]))
                        {
                            Close();
                            return;
                        }
                        IsSocketOpen = true;
                    }
                        
                }

            }
            catch (Exception e)
            {
                CaughtFailure.exception = e;
            }

            }

        private bool GetInitialConnectedUsers(string number)
        {
            //originally this was a for loop however this became an issue of the client
            //checking too fast for the server meaning when more than one other client
            //was connected the ability for the client to list them became spotty a
            //while loop clears this up
            int maxAttempts = 1000;
            int attempts = 0;
            int i = 0;
            if (int.TryParse(number, out int numUsers))
            {
                
                while (i < numUsers && attempts < maxAttempts)
                {

                    (int? responseLength, byte[] response) = ReceiveMessage(100);
                    if (responseLength != 0 && responseLength != null)
                    {
                        Users.Add(response[0..(int)responseLength]);
                        i++;
                    }
                    attempts++;
                }
            }
            if (i == numUsers)
            {
                return true;
            }
            CaughtFailure.errorResponse = $"Couldn't collect all {numUsers} of the connected users after {attempts} number of attempts";
            return false;
        }
        public void SendBattleRequest(byte[] user)
        {
            var msg = new byte[1 + user.Length];
            msg[0] = 8; //this is equvilant to \x08 
            Array.Copy(user, 0, msg, 1, user.Length);
            SendMessage(msg);
            BattleRequestsSent.Add(user);
        }
        public void DeclineBattleRequest(byte[] user)
        {
            var msg = new byte[1 + user.Length];
            msg[0] = 9; //this is equvilant to \x09
            Array.Copy(user, 0, msg, 1, user.Length);
            SendMessage(msg);
        }
        public void AcceptBattleRequest(byte[] user)
        {
            var msg = new byte[1 + user.Length];
            msg[0] = 10; //this is equvilant to \x0A
            Array.Copy(user, 0, msg, 1, user.Length);
            SendMessage(msg);
        }

        //NOTE: I have no idea why these functions are needed
        //int index = Program.MainServerNetworkManager.Users.IndexOf(response[1..(int)(responseLength)].ToArray()) would be used but for some reason
        // the byte[] is different yet as a string they're the same
        public int GetIndexInUsers(byte[] user)
        {
            var stringName = Encoding.UTF8.GetString(user);
            var index = -1;
            for (var i = 0; i < Program.MainServerNetworkManager.Users.Count; i++)
            {
                if (Encoding.UTF8.GetString(Program.MainServerNetworkManager.Users[i]) == stringName)
                {
                    index = i;
                }
            }
            return index;
        }
        public int GetIndexInReceivedRequests(byte[] user)
        {
            var stringName = Encoding.UTF8.GetString(user);
            var index = -1;
            for (var i = 0; i < Program.MainServerNetworkManager.BattleRequestsRecieved.Count; i++)
            {
                if (Encoding.UTF8.GetString(Program.MainServerNetworkManager.BattleRequestsRecieved[i]) == stringName)
                {
                    index = i;
                }
            }
            return index;
        }

        public int GetIndexInRequestsSent(byte[] user)
        {
            var stringName = Encoding.UTF8.GetString(user);
            var index = -1;
            for (var i = 0; i < Program.MainServerNetworkManager.BattleRequestsSent.Count; i++)
            {
                if (Encoding.UTF8.GetString(Program.MainServerNetworkManager.BattleRequestsSent[i]) == stringName)
                {
                    index = i;
                }
            }
            return index;
        }
    }
}

