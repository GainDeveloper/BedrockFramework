/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework


********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System;
using System.Collections.Generic;

namespace BedrockFramework.Network
{
    public interface INetworkService
    {
        void StartHost(bool internetHost);
        void StartClient(string remoteHost);
        void Stop();

        event Action OnBecomeHost;

        event Action<NetworkConnection> OnNewNetworkConnection;
        event Action<NetworkConnection> OnNetworkConnectionReady;

        event Action OnStop;

        /// <summary>
        /// Returns true when either not online or is host.
        /// </summary>
        bool IsHost { get; }
        bool IsActive { get; }
        NetworkSocket ActiveSocket { get; }

        short UniqueNetworkID { get; }
        NetworkGameObject GetNetworkGameObject(short id);
        void AddNetworkGameObject(NetworkGameObject netObj);
        void RemoveNetworkGameObject(NetworkGameObject netObj);
    }

    public class NullNetworkService : INetworkService
    {
        public void StartHost(bool internetHost) { }
        public void StartClient(string remoteHost) { }
        public void Stop() { }

        public event Action OnBecomeHost = delegate { };

        public event Action<NetworkConnection> OnNewNetworkConnection = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionReady = delegate { };

        public event Action OnStop = delegate { };

        public bool IsHost { get { return true; } }
        public bool IsActive { get { return false; } }
        public NetworkSocket ActiveSocket { get { return null; } }

        public short UniqueNetworkID { get { return 0; } }
        public NetworkGameObject GetNetworkGameObject(short id) { return null; }
        public void AddNetworkGameObject(NetworkGameObject netObj) { }
        public void RemoveNetworkGameObject(NetworkGameObject netObj) { }
    }

    public class NetworkService : Service, INetworkService
    {
        public const string NetworkLog = "Network";
        public const string NetworkIncomingLog = "Incoming Data";
        public const string NetworkOutgoingLog = "Outgoing Data";
        public const int MaxConnections = 1;

        private int hostPort = 7777;
        private string localHost = "127.0.0.1";
        private NetworkMatch networkMatch;
        private NetworkSocket currentSocket;
        private short nextUniqueNetworkID = 1;
        private Dictionary<short, NetworkGameObject> activeNetworkGameObjects = new Dictionary<short, NetworkGameObject>();

        public bool IsActive { get { return currentSocket != null && currentSocket.IsActive; } }
        public event Action OnBecomeHost = delegate { };
        public event Action<NetworkConnection> OnNewNetworkConnection = delegate { };
        public event Action<NetworkConnection> OnNetworkConnectionReady = delegate { };
        public event Action OnStop = delegate { };

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

        public NetworkGameObject GetNetworkGameObject(short id) { return activeNetworkGameObjects.ContainsKey(id) ? activeNetworkGameObjects[id] : null; }
        public void AddNetworkGameObject(NetworkGameObject netObj) { activeNetworkGameObjects[netObj.NetworkID] = netObj; }
        public void RemoveNetworkGameObject(NetworkGameObject netObj) { activeNetworkGameObjects.Remove(netObj.NetworkID); }

        public NetworkService(MonoBehaviour owner) : base(owner)
        {
            DevTools.DebugMenu.AddDebugButton("Network", "Host Internet", () => { StartHost(true); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugButton("Network", "Host Local", () => { StartHost(false); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugButton("Network", "Join Internet", () => { JoinFirstInternetMatch(); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugButton("Network", "Join Local", () => { StartClient(localHost); }, () => { return !IsActive; });
            DevTools.DebugMenu.AddDebugButton("Network", "Send Test", () => { SendTestMessage(); }, () => { return IsActive; });
            DevTools.DebugMenu.AddDebugButton("Network", "Leave", () => { Stop(); }, () => { return IsActive; });
            DevTools.DebugMenu.AddDebugStats("Network Stats", NetworkStats);

            networkMatch = owner.gameObject.AddComponent<NetworkMatch>();
        }

        IEnumerable<string> NetworkStats()
        {
            byte error;

            yield return "Active: " + IsActive;
            if (IsActive)
            {
                yield return "Host: " + IsHost;
                foreach (NetworkConnection connection in ActiveSocket.ActiveConnections())
                {
                    string connectionStat = connection.ConnectionID.ToString();
                    if (connection.IsLocalConnection)
                        connectionStat += " (Local)";
                    connectionStat += " : " + connection.CurrentState;
                    //connectionStat += " (" + NetworkTransport.GetCurrentRTT(currentSocket.SocketID, connection.ConnectionID, out error) + "MS)";
                    yield return connectionStat;
                }


                yield return ActiveSocket.BytesPerSecond.ToString() + " B/s";
                yield return ActiveSocket.MessagesPerSecond.ToString() + " M/s";
            }
        }

        //
        // Hosting
        //

        public void StartHost(bool internetHost)
        {
            if (IsActive)
                return;

            DevTools.Logger.Log(NetworkLog, "Starting Host");

            if (internetHost)
                networkMatch.CreateMatch("BedrockHost", MaxConnections + 1, true, "", "", "", 0, 0, OnMatchCreate);
            else
            {
                NewSocket();
                if (currentSocket.Startup(hostPort, MaxConnections))
                    OnBecomeHost();
            }
        }

        // Called when relay server has created a match for us.
        void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if (success)
            {
                DevTools.Logger.Log(NetworkLog, "Create Relay Match Succeeded");
                Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);

                NewSocket();
                if (currentSocket.Startup(hostPort, MaxConnections))
                {
                    currentSocket.StartupRelayHost(matchInfo);
                    OnBecomeHost();
                } 
            }
            else
            {
                DevTools.Logger.LogError(NetworkLog, "Create relay match failed: {}", () => new object[] { extendedInfo });
            }
        }

        //
        // Clients
        //

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

        public void JoinFirstInternetMatch()
        {
            if (IsActive)
                return;

            networkMatch.ListMatches(0, 1, "", true, 0, 0, (success, info, matches) =>
            {
                if (success && matches.Count > 0)
                    networkMatch.JoinMatch(matches[0].networkId, "", "", "", 0, 0, OnMatchJoined);
            });
        }

        void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            if (success)
            {
                DevTools.Logger.Log(NetworkLog, "Join Relay Match Succeeded");
                Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);

                //m_MatchInfo = matchInfo;
                NewSocket();
                if (currentSocket.Startup(0, 1))
                {
                    currentSocket.ConnectThroughRelay(matchInfo);
                }
            }
            else
            {
                DevTools.Logger.LogError(NetworkLog, "Join relay match failed: {}", () => new object[] { extendedInfo });            }
        }

        //
        // Generic
        //

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
            currentSocket.OnNewNetworkConnection -= OnNewNetworkConnection;
            currentSocket.OnNetworkConnectionReady -= OnNetworkConnectionReady;
            currentSocket.OnShutdown -= CurrentSocket_OnShutdown;
        }

        public void SendTestMessage()
        {
            if (!IsActive)
                return;

            if (currentSocket.IsHost)
            {
                foreach (NetworkConnection connection in currentSocket.ActiveConnections())
                {
                    NetworkWriter writer = currentSocket.Writer.Setup(currentSocket.UnreliableChannel, MessageTypes.BRF_DebugTest);
                    currentSocket.Writer.Send(connection, () => "Debug Test");
                }
            }
            else
            {
                NetworkWriter writer = currentSocket.Writer.Setup(currentSocket.UnreliableChannel, MessageTypes.BRF_DebugTest);
                currentSocket.Writer.Send(currentSocket.LocalConnection, () => "Debug Test");
            }
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            OnStop();
            currentSocket.SendDisconnect();
        }
    }
}