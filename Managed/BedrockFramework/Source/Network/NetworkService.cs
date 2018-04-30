/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework


********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace BedrockFramework.Network
{
    public interface INetworkService
    {
        void StartHost();
        void StartClient();
        void Stop();
    }

    public class NullNetworkService : INetworkService
    {
        public void StartHost() { }
        public void StartClient() { }
        public void Stop() { }
    }

    public class NetworkService : Service, INetworkService
    {
        public const string NetworkLog = "Network";
        public const int MaxPlayers = 2;

        private int hostPort = 7777;
        private string remoteHost = "127.0.0.1";
        private NetworkSocket currentSocket;

        public bool IsActive { get { return currentSocket != null; } }

        public NetworkService(MonoBehaviour owner) : base(owner)
        {
            DevTools.DebugMenu.AddDebugItem("Network", "Host", () => { StartHost(); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugItem("Network", "Join", () => { StartClient(); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugItem("Network", "Leave", () => { Stop(); }, () => { return IsActive; });
        }


        public void StartHost()
        {
            if (IsActive)
                return;

            DevTools.Logger.Log(NetworkLog, "Starting Host");
            currentSocket = new NetworkSocket(owner);
            currentSocket.Startup(hostPort, MaxPlayers);
        }

        public void StartClient()
        {
            if (IsActive)
                return;

            DevTools.Logger.Log(NetworkLog, "Starting Client");
            currentSocket = new NetworkSocket(owner);
            if (currentSocket.Startup(0, 1))
            {
                currentSocket.Connect(remoteHost, hostPort);
            }
        }



        public void Stop()
        {
            if (!IsActive)
                return;

            currentSocket.Disconnect();
            // If we are host we should tell everyone else to disconnect before closing the socket.
            // Otherwise just send a disconnect event.
        }
    }
}