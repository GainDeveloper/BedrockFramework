/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework


********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace BedrockFramework.Network
{
    public interface INetworkService
    {
        void StartHost();
        void StartClient(string remoteHost);
        void Stop();

        event Action<NetworkConnection> OnNewNetworkConnection;
        event Action<NetworkConnection> OnNetworkConnectionReady;

        /// <summary>
        /// Returns true when either not online or is host.
        /// </summary>
        bool IsHost { get; }
        bool IsActive { get; }
        NetworkSocket ActiveSocket { get; }

        short UniqueNetworkID { get; }
    }

    public class NullNetworkService : INetworkService
    {
        public void StartHost() { }
        public void StartClient(string remoteHost) { }
        public void Stop() { }

        public event Action<NetworkConnection> OnNewNetworkConnection = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionReady = delegate { };

        public bool IsHost { get { return true; } }
        public bool IsActive { get { return false; } }
        public NetworkSocket ActiveSocket { get { return null; } }

        public short UniqueNetworkID { get { return 0; } }
    }

    public class NetworkService : Service, INetworkService
    {
        public const string NetworkLog = "Network";
        public const int MaxConnections = 1;

        private int hostPort = 7777;
        private string localHost = "127.0.0.1";
        private NetworkSocket currentSocket;
        private short nextUniqueNetworkID = 1;

        public bool IsActive { get { return currentSocket != null && currentSocket.IsActive; } }
        public event Action<NetworkConnection> OnNewNetworkConnection = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionReady = delegate { };

        public bool IsHost {
            get {
                if (!IsActive)
                    return true;
                else
                {
                    return currentSocket.IsHost;
                }
            }
        }

        public NetworkSocket ActiveSocket {
            get
            {
                if (!IsActive)
                    return null;
                return currentSocket;
            }
        }

        public short UniqueNetworkID
        {
            get
            {
                short cachedNetworkID = nextUniqueNetworkID;
                nextUniqueNetworkID++;
                return cachedNetworkID;
            }
        }

        public NetworkService(MonoBehaviour owner) : base(owner)
        {
            DevTools.DebugMenu.AddDebugItem("Network", "Host", () => { StartHost(); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugItem("Network", "Join", () => { StartClient(localHost); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugItem("Network", "Send Test", () => { SendTestMessage(); }, () => { return IsActive; });
            DevTools.DebugMenu.AddDebugItem("Network", "Leave", () => { Stop(); }, () => { return IsActive; });
        }

        public void StartHost()
        {
            if (IsActive)
                return;

            DevTools.Logger.Log(NetworkLog, "Starting Host");

            NewSocket();
            currentSocket.Startup(hostPort, MaxConnections);
        }

        public void StartClient(string remoteHost)
        {
            if (IsActive)
                return;

            DevTools.Logger.Log(NetworkLog, "Starting Client");

            NewSocket();
            if (currentSocket.Startup(0, 1))
            {
                currentSocket.Connect(remoteHost, hostPort);
            }
        }

        public void SendTestMessage()
        {
            if (!IsActive)
                return;

            if (currentSocket.IsHost)
            {
                foreach (NetworkConnection connection in currentSocket.ActiveConnections())
                {
                    NetworkWriter writer = currentSocket.Writer.Setup(connection, currentSocket.ReliableSequencedChannel, MessageTypes.BRF_TestString);
                    writer.Write("Test To Client");
                    currentSocket.Writer.Send();
                }
            } else
            {
                NetworkWriter writer = currentSocket.Writer.Setup(currentSocket.LocalConnection, currentSocket.ReliableSequencedChannel, MessageTypes.BRF_TestString);
                writer.Write("Test To Server");
                currentSocket.Writer.Send();
            }
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            currentSocket.SendDisconnect();
        }

        private void NewSocket()
        {
            currentSocket = new NetworkSocket(owner);
            currentSocket.OnNewNetworkConnection += CurrentSocket_OnNewNetworkConnection;
            currentSocket.OnNetworkConnectionReady += CurrentSocket_OnNetworkConnectionReady;
            currentSocket.OnShutdown += CurrentSocket_OnShutdown;
        }

        private void CurrentSocket_OnNewNetworkConnection(NetworkConnection networkConnection)
        {
            OnNewNetworkConnection(networkConnection);
        }

        private void CurrentSocket_OnNetworkConnectionReady(NetworkConnection networkConnection)
        {
            OnNetworkConnectionReady(networkConnection);
        }

        private void CurrentSocket_OnShutdown()
        {
            currentSocket.OnNewNetworkConnection -= CurrentSocket_OnNewNetworkConnection;
            currentSocket.OnNetworkConnectionReady -= CurrentSocket_OnNetworkConnectionReady;
            currentSocket.OnShutdown -= CurrentSocket_OnShutdown;
        }
    }
}