/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Receives all data from the host.
Stores list of connections.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;
using System;

namespace BedrockFramework.Network
{
    public class NetworkSocket
    {
        private MonoBehaviour owner;
        private Dictionary<int, NetworkConnection> activeConnections = new Dictionary<int, NetworkConnection>();

        private ConnectionConfig connectionConfig;
        private int socketID = -1;
        private int localConnectionID = 0;
        private MatchInfo matchInfo = null;
        private Coroutine eventPoll, statCounter;
        
        public bool IsHost { get { return localConnectionID == 0; } }
        public bool IsActive { get { return socketID != -1; } }
        public int SocketID { get { return socketID; } }
        public int LocalConnectionID { get { return localConnectionID; } }

        // Writer
        private byte[] receivedDataBuffer = new byte[1024];
        private NetworkWriterWrapper writer;
        public NetworkWriterWrapper Writer { get { return writer; } }

        // Channels
        private int reliableSequencedChannel, reliableChannel, unreliableChannel;
        public int ReliableSequencedChannel { get { return reliableSequencedChannel; } }
        public int ReliableChannel { get { return reliableChannel; } }
        public int UnreliableChannel { get { return unreliableChannel; } }

        // Stats
        private int bytesPerSecond = 0, messagesPerSecond = 0;
        public int BytesPerSecond { get { return bytesPerSecond; } }
        public int MessagesPerSecond { get { return messagesPerSecond; } }

        public NetworkConnection LocalConnection { get { return !activeConnections.ContainsKey(localConnectionID) ? null : activeConnections[localConnectionID]; } }
        public IEnumerable<NetworkConnection> ActiveConnections() {
            foreach (NetworkConnection connection in activeConnections.Values)
                yield return connection;
        }
        public int NumActiveConnections { get { return activeConnections.Count; } }

        public event Action<NetworkConnection> OnNewNetworkConnection = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionReady = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionDisconnected = delegate { };
        public event Action OnShutdown = delegate { };

        public NetworkSocket(MonoBehaviour owner)
        {
            this.owner = owner;
            this.writer = new NetworkWriterWrapper(this);

            Application.runInBackground = true; // We force this as server should never suspend.
            SetupConfig();
        }

        void SetupConfig()
        {
            GlobalConfig globalConfig = new GlobalConfig();
            connectionConfig = new ConnectionConfig();
            reliableSequencedChannel = connectionConfig.AddChannel(QosType.ReliableSequenced);
            reliableChannel = connectionConfig.AddChannel(QosType.Reliable);
            unreliableChannel = connectionConfig.AddChannel(QosType.Unreliable);
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
            if (Debug.isDebugBuild)
                statCounter = owner.StartCoroutine(SecondStatsCounter());
            return true;
        }

        public void StartupRelayHost(MatchInfo matchInfo)
        {
            this.matchInfo = matchInfo;

            byte error;
            NetworkTransport.ConnectAsNetworkHost(
                socketID, matchInfo.address, matchInfo.port, matchInfo.networkId, Utility.GetSourceID(), matchInfo.nodeId, out error);
        }

        //
        // Client
        //

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

        public void ConnectThroughRelay(MatchInfo matchInfo)
        {
            if (localConnectionID != 0)
            {
                // Already connected.
                return;
            }

            this.matchInfo = matchInfo;

            byte errorCode;
            localConnectionID = NetworkTransport.ConnectToNetworkPeer(socketID, matchInfo.address, matchInfo.port, 0, 0, matchInfo.networkId, Utility.GetSourceID(), matchInfo.nodeId, out errorCode);

            if (errorCode == 0)
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Client Connection ID: {}", () => new object[] { localConnectionID });
            }
            else
            {
                Debug.LogError(errorCode);
            }
        }

        //
        // Lifetime
        //

        IEnumerator PollNetworkEvents()
        {
            //TODO: Need to handle timeouts. They don't come with any specific connection id.
            while (IsActive)
            {
                // Get events from the relay connection
                byte error;
                NetworkEventType networkEvent;

                networkEvent = NetworkTransport.ReceiveRelayEventFromHost(socketID, out error);
                if (networkEvent == NetworkEventType.ConnectEvent)
                    DevTools.Logger.Log(NetworkService.NetworkLog, "Relay Server Connected");
                if (networkEvent == NetworkEventType.DisconnectEvent)
                    DevTools.Logger.Log(NetworkService.NetworkLog, "Relay Server Disconnected");

                // Get events from host connection
                int connectionId;
                int channelId;
                int receivedSize;
                networkEvent = NetworkTransport.ReceiveFromHost(socketID, out connectionId, out channelId, receivedDataBuffer, (ushort)receivedDataBuffer.Length, out receivedSize, out error);

                while (networkEvent != NetworkEventType.Nothing)
                {

                    if (error != 0)
                    {
                        DevTools.Logger.LogError(NetworkService.NetworkLog, "{}: ", () => new object[] { (NetworkError)error, connectionId });
                        Close(); //Do we actually want to CLOSE the connection if we hit an error? Perhaps on clients but not on host.
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

        public void SendData(NetworkConnection connection, int channelId, byte[] data, int dataSize, Func<string> dataSendType)
        {
            if (IsHost)
            {
                connection.SendData(channelId, data, dataSize, dataSendType);
            } else
            {
                if (connection != LocalConnection)
                {
                    DevTools.Logger.Log(NetworkService.NetworkLog, "Attempting to send data to a none local connection. This is not allowed.");
                    return;
                }

                connection.SendData(channelId, data, dataSize, dataSendType);
            }
        }

        int TotalBytes()
        {
            byte error;
            return NetworkTransport.GetOutgoingFullBytesCountForHost(socketID, out error);
        }

        IEnumerator SecondStatsCounter()
        {
            int lastTotalBytes = TotalBytes();
            int lastTotalMessages = NetworkTransport.GetOutgoingMessageCount();

            while (true)
            {
                yield return new WaitForSecondsRealtime(1);
                bytesPerSecond = TotalBytes() - lastTotalBytes;
                lastTotalBytes = TotalBytes();

                messagesPerSecond = NetworkTransport.GetOutgoingMessageCount() - lastTotalMessages;
                lastTotalMessages = NetworkTransport.GetOutgoingMessageCount();
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
            activeConnections[connectionId].OnReady += NetworkSocket_OnReady;
            OnNewNetworkConnection(activeConnections[connectionId]);
        }

        private void NetworkSocket_OnReady(NetworkConnection obj)
        {
            OnNetworkConnectionReady(obj);
        }

        void RemoveConnection(int connectionId)
        {
            if (!activeConnections.ContainsKey(connectionId))
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Received disconnect for connection that does not exist.");
                return;
            }

            activeConnections[connectionId].Disconnect();
            OnNetworkConnectionDisconnected(activeConnections[connectionId]);
            activeConnections[connectionId].OnReady -= NetworkSocket_OnReady;
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
                // Should probably make the network match unjoinable
                foreach (NetworkConnection connection in activeConnections.Values)
                {
                    byte error;
                    NetworkTransport.Disconnect(socketID, connection.ConnectionID, out error);
                }
                owner.StartCoroutine(HostWaitClosedConnections());
            }
        }

        public void OnConnectionDropped(bool success, string extendedInfo)
        {
            Debug.Log("Connection has been dropped on matchmaker server");
        }

        public void Close()
        {
            owner.StartCoroutine(ClientWaitShutdown());
        }

        // We wait one frame before closing the socket when we are a client.
        // This is so the server has time to receive the close event.
        IEnumerator ClientWaitShutdown()
        {
            if (matchInfo != null)
                yield return owner.GetComponent<NetworkMatch>().DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, OnDropConnection);

            yield return null;
            Shutdown();
        }

        void OnDropConnection(bool success, string extendedInfo) { }

        // We wait for all current connections to close before shutting down the server.
        // TODO: We should add a timeout for this incase any clients don't respond.
        IEnumerator HostWaitClosedConnections()
        {
            while (activeConnections.Count > 0)
            {
                yield return null;
            }

            Close();
        }

        public void Shutdown()
        {
            NetworkTransport.RemoveHost(socketID);
            socketID = -1;
            localConnectionID = 0;
            matchInfo = null;

            owner.StopCoroutine(eventPoll);
            if (statCounter != null)
            {
                owner.StopCoroutine(statCounter);
                statCounter = null;
            }

            NetworkTransport.Shutdown();
            OnShutdown();
        }
    }
}