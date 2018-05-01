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
        private Dictionary<int, NetworkConnection> activeConnections = new Dictionary<int, NetworkConnection>();

        private ConnectionConfig connectionConfig;
        private int reliableSequencedChannel;
        private int socketID = -1;
        private int localConnectionID = 0;
        private Coroutine eventPoll;
        private byte[] receivedDataBuffer = new byte[1024];

        public bool IsHost { get { return localConnectionID == 0; } }
        public bool IsActive { get { return socketID != -1; } }
        public int SocketID { get { return socketID; } }
        public int LocalConnectionID { get { return localConnectionID; } }
        public int ReliableSequencedChannel { get { return reliableSequencedChannel; } }

        public NetworkConnection LocalConnection { get { return activeConnections[localConnectionID]; } }
        public IEnumerable<NetworkConnection> ActiveConnections() {
            foreach (NetworkConnection connection in activeConnections.Values)
                yield return connection;
        }

        //public NetworkMessage NetworkMessage { get { return networkMessage; } }

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


            socketID = NetworkTransport.AddHost(topology, port);
            if (socketID == -1)
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
            localConnectionID = NetworkTransport.Connect(socketID, remoteHost, hostPort, 0, out errorCode);
            if (errorCode == 0)
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Client Connection ID: {}", () => new object[] { localConnectionID });
            } else
            {
                Debug.LogError(errorCode);
            }
        }

        //
        // Lifetime
        //

        IEnumerator PollNetworkEvents()
        {
            while (IsActive)
            {
                int connectionId;
                int channelId;
                int receivedSize;
                byte error;
                NetworkEventType networkEvent = NetworkTransport.ReceiveFromHost(socketID, out connectionId, out channelId, receivedDataBuffer, (ushort)receivedDataBuffer.Length, out receivedSize, out error);

                while (networkEvent != NetworkEventType.Nothing)
                {

                    if (error != 0)
                    {
                        DevTools.Logger.LogError(NetworkService.NetworkLog, "{}: ", () => new object[] { (NetworkError)error, connectionId });
                        Close();
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
                            activeConnections[connectionId].ReceiveData(channelId, receivedDataBuffer, receivedSize);
                            break;
                        case NetworkEventType.DisconnectEvent:
                            RemoveConnection(connectionId);
                            break;
                        case NetworkEventType.BroadcastEvent:
                            break;
                    }

                    networkEvent = NetworkTransport.ReceiveFromHost(socketID, out connectionId, out channelId, receivedDataBuffer, (ushort)receivedDataBuffer.Length, out receivedSize, out error);
                }

                yield return null;
            }
        }

        public void SendData(NetworkConnection connection, int channelId, byte[] data, int dataSize)
        {
            if (IsHost)
            {
                connection.SendData(channelId, data, dataSize);
            } else
            {
                if (connection != LocalConnection)
                {
                    DevTools.Logger.Log(NetworkService.NetworkLog, "Attempting to send data to a none local connection. This is not allowed.");
                    return;
                }

                connection.SendData(channelId, data, dataSize);
            }
        }

        //
        // Manage Network Connections
        //

        void SetupConnection(int connectionId)
        {
            if (activeConnections.ContainsKey(connectionId))
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Received new connection for existing connection");
                return;
            }

            activeConnections[connectionId] = new NetworkConnection(this, connectionId);
        }

        void RemoveConnection(int connectionId)
        {
            if (!activeConnections.ContainsKey(connectionId))
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Received disconnect for connection that does not exist.");
                return;
            }

            activeConnections[connectionId].Disconnect();
            activeConnections.Remove(connectionId);
        }

        //
        // Disconnections
        //

        public void SendDisconnect()
        {
            if (!IsActive)
                return;

            if (!IsHost)
            {
                byte error;
                NetworkTransport.Disconnect(socketID, localConnectionID, out error);
            } else
            {
                foreach (NetworkConnection connection in activeConnections.Values)
                {
                    byte error;
                    NetworkTransport.Disconnect(socketID, connection.ConnectionID, out error);
                }
                owner.StartCoroutine(HostWaitClosedConnections());
            }
        }

        public void Close()
        {
            owner.StartCoroutine(ClientWaitShutdown());
        }

        // We wait one frame before closing the socket when we are a client.
        // This is so the server has time to receive the close event.
        IEnumerator ClientWaitShutdown()
        {
            yield return null;
            Shutdown();
        }

        // We wait for all current connections to close before shutting down the server.
        // TODO: We should add a timeout for this incase any clients don't respond.
        IEnumerator HostWaitClosedConnections()
        {
            while (activeConnections.Count > 0)
            {
                yield return null;
            }

            Shutdown();
        }

        public void Shutdown()
        {
            NetworkTransport.RemoveHost(socketID);
            socketID = -1;
            localConnectionID = 0;

            NetworkTransport.Shutdown();
        }
    }
}