/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Receives all data from the host.
Stores list of connections.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace BedrockFramework.Network
{
    public class NetworkSocket
    {
        private MonoBehaviour owner;

        private ConnectionConfig connectionConfig;
        private int reliableSequencedChannel;
        private int serverID = -1;
        private int localConnectionID = 0;

        bool IsHost { get { return localConnectionID == 0; } }
        bool IsActive { get { return serverID != -1; } }

        private Coroutine eventPoll;
        private byte[] receivedDataBuffer = new byte[1024];

        private Dictionary<int, NetworkConnection> activeConnections = new Dictionary<int, NetworkConnection>();

        public NetworkSocket(MonoBehaviour owner)
        {
            this.owner = owner;
            Application.runInBackground = true; // We force this as server should never suspend.
            SetupConfig();
        }

        void SetupConfig()
        {
            GlobalConfig globalConfig = new GlobalConfig();
            connectionConfig = new ConnectionConfig();
            reliableSequencedChannel = connectionConfig.AddChannel(QosType.ReliableSequenced);
            NetworkTransport.Init(globalConfig);
        }

        public bool Startup(int port, int maxConnections)
        {
            HostTopology topology = new HostTopology(connectionConfig, maxConnections);


            serverID = NetworkTransport.AddHost(topology, port);
            if (serverID == -1)
            {
                //SERVER FAILED
                return false;
            }

            eventPoll = owner.StartCoroutine(PollNetworkEvents());
            return true;
        }

        public void Connect(string remoteHost, int hostPort)
        {
            if (localConnectionID != 0)
            {
                // Already connected.
                return;
            }

            byte errorCode;
            localConnectionID = NetworkTransport.Connect(serverID, remoteHost, hostPort, 0, out errorCode);
            if (errorCode == 0)
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Client Connection ID: {}", () => new object[] { localConnectionID });
            } else
            {
                Debug.LogError(errorCode);
            }
        }


        IEnumerator PollNetworkEvents()
        {
            while (IsActive)
            {
                int connectionId;
                int channelId;
                int receivedSize;
                byte error;
                NetworkEventType networkEvent = NetworkTransport.ReceiveFromHost(serverID, out connectionId, out channelId, receivedDataBuffer, (ushort)receivedDataBuffer.Length, out receivedSize, out error);

                while (networkEvent != NetworkEventType.Nothing)
                {

                    if (error != 0)
                    {
                        DevTools.Logger.LogError(NetworkService.NetworkLog, "{}: ", () => new object[] { (NetworkError)error, connectionId });
                        break;
                    }
                    if (networkEvent == NetworkEventType.Nothing)
                        break;

                    switch (networkEvent)
                    {
                        case NetworkEventType.ConnectEvent:
                            SetupConnection(connectionId);
                            break;
                        case NetworkEventType.DataEvent:
                            // Pass received data to connection.
                            break;
                        case NetworkEventType.DisconnectEvent:
                            RemoveConnection(connectionId);
                            break;
                        case NetworkEventType.BroadcastEvent:
                            break;
                    }

                    networkEvent = NetworkTransport.ReceiveFromHost(serverID, out connectionId, out channelId, receivedDataBuffer, (ushort)receivedDataBuffer.Length, out receivedSize, out error);
                }

                yield return null;
            }
        }

        void SetupConnection(int connectionId)
        {
            if (activeConnections.ContainsKey(connectionId))
            {
                Debug.LogError("Received new connection for existing connection");
                return;
            }

            activeConnections[connectionId] = new NetworkConnection(connectionId);
        }

        void RemoveConnection(int connectionId)
        {
            if (!activeConnections.ContainsKey(connectionId))
            {
                Debug.LogError("Received disconnect for connection that does not exist.");
                return;
            }

            activeConnections[connectionId].Disconnect();
            activeConnections.Remove(connectionId);
        }

        public void Disconnect()
        {
            if (!IsActive)
                return;

            if (!IsHost)
            {
                byte error;
                NetworkTransport.Disconnect(serverID, localConnectionID, out error);
            }
        }

        public void Shutdown()
        {
            // Actually close the socket and shutdown the network transport.
        }
    }
}