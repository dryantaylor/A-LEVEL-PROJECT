using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace PokemonBattleSimulator.EngineFramework.Networking
{
    public abstract class ServerNetworkManager
    {
        internal Socket ConnSocket;
        public bool IsSocketOpen = false;
        public byte[] Username { get; internal set; }
        public IPAddress Host { get; internal set; }
        internal (Exception exception, string? errorResponse) CaughtFailure = (null, null);

        public ServerNetworkManager(string hostString, int port,byte[] username)
        {
            Username = username;
            try
            {
                Host = IPAddress.Parse(hostString);
                var endPoint = new IPEndPoint(Host, port);

                ConnSocket = new Socket(Host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    ConnSocket.Connect(endPoint);
                    IsSocketOpen = true;
                }
                catch (Exception e)
                {
                    CaughtFailure.exception = e;
                }

            }
            catch (Exception e)
            {
                CaughtFailure.exception = e;
            }
        }

        public bool IsAlive()
        {
            if (!IsSocketOpen) { return false; }
            SendMessage(new byte[] { 1 }); //this is 0x01
            var (responseLength, response) = ReceiveMessage(10000);
            if (responseLength != 0 && responseLength != null)
            {
                return true;
            }
            return false;
        }

        public (Exception exception, string? errorResponse) GetFailure()
        {
            return CaughtFailure;
        }
        public void Close()
        {
            Console.WriteLine("Socket closed");
            // Release the socket.
            ConnSocket.Disconnect(false);
            ConnSocket.Close();
            IsSocketOpen = false;
        }
     
        internal void SendMessage(byte[] message)
        {
            //IMPLEMENTATION NOTE: If ported to be cross platform ensure that the data is converted into little endian not big endian
            //all messages have the first 8 bytes be the byte length of the message
            var msg = new byte[4 + message.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(message.Length), 0, msg, 0, 4);
            Buffer.BlockCopy(message, 0, msg, 4, message.Length);
            ConnSocket.Send(msg);
        }

        public (int? responseLength, byte[] response) ReceiveMessage(int pollTime = -1)
        {
            try
            {
                //use a byte?[] rather than a byte[] if possible
                var response = new List<byte>();
                if (!ConnSocket.Poll(pollTime, SelectMode.SelectRead)) //waits for pollTime milliseconds to see if there is a message awaiting reading
                {
                    return (0, Array.Empty<byte>());
                }

                var rcvBuffer = new byte[4]; //this is getting the length of the message
                ConnSocket.Receive(rcvBuffer);
                var length = BitConverter.ToInt32(rcvBuffer); //little endian
                rcvBuffer = new byte[1024];
                while (response.Count < length)
                {
                    ConnSocket.Receive(rcvBuffer);
                    if (rcvBuffer == new byte[1024])
                    {
                        break;
                    }
                    if (response.Count + 1024 > length) //cuts empty space in the received data
                    {
                        response.AddRange(rcvBuffer.AsSpan(0, length - response.Count).ToArray());
                        break;
                    }
                    response.AddRange(rcvBuffer);
                }
                ConnSocket.Blocking = true;
                return new ValueTuple<int, byte[]>(length, response.ToArray());
            }
            catch (SocketException e)
            {
                return new ValueTuple<int?, byte[]>(null, Array.Empty<byte>());
            }

        }
    }
}


