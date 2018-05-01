/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework


********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace BedrockFramework.Network
{
    public interface INetworkService
    {
        void StartHost();
        void StartClient(string remoteHost);
        void Stop();

        //void NewConnection(NetworkConnection newConnection);
        //void NewConnectionReady(NetworkConnection readyConnection); 
    }

    public class NullNetworkService : INetworkService
    {
        public void StartHost() { }
        public void StartClient(string remoteHost) { }
        public void Stop() { }
    }

    public class NetworkService : Service, INetworkService
    {
        public const string NetworkLog = "Network";
        public const int MaxConnections = 1;

        private int hostPort = 7777;
        private string localHost = "127.0.0.1";
        private NetworkSocket currentSocket;

        public bool IsActive { get { return currentSocket != null && currentSocket.IsActive; } }

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
            currentSocket = new NetworkSocket(owner);
            currentSocket.Startup(hostPort, MaxConnections);
        }

        public void StartClient(string remoteHost)
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

        public void SendTestMessage()
        {
            if (!IsActive)
                return;

            if (currentSocket.IsHost)
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Test To Client");
                foreach (NetworkConnection connection in currentSocket.ActiveConnections())
                    currentSocket.SendData(connection, currentSocket.ReliableSequencedChannel, bytes, bytes.Length);
            } else
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Test To Server");
                currentSocket.SendData(currentSocket.LocalConnection, currentSocket.ReliableSequencedChannel, bytes, bytes.Length);
            }
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            currentSocket.SendDisconnect();
        }
    }
}