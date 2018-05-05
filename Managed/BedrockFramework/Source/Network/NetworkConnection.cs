/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Represents a connection between a client and a server.
Holds the current status of the connection.
Holds basic info about the connection.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using ProtoBuf;
using System;
using System.IO;
using Sirenix.OdinInspector;

namespace BedrockFramework.Network
{
    public enum NetworkConnectionState
    {
        Loading,
        Waiting,
        Ready,
        Disconnecting
    }

    public class NetworkConnection
    {
        [ReadOnly, ShowInInspector]
        int connectionID;
        NetworkSocket networkSocket;
        NetworkConnectionState currentState = NetworkConnectionState.Loading;

        private string friendlyName = "Unnamed";
        public string FriendlyName { get { return friendlyName; } }

        public int ConnectionID { get { return connectionID; } }
        /// <summary>
        /// Are we the one that initiated this connection.
        /// </summary>
        public bool IsLocalConnection { get { return connectionID == this.networkSocket.LocalConnectionID; } }

        public NetworkConnectionState CurrentState { get { return currentState; } }

        public event Action<NetworkConnection> OnReady = delegate { };

        public NetworkConnection(NetworkSocket networkSocket, int connectionID)
        {
            this.connectionID = connectionID;
            this.networkSocket = networkSocket;
            DevTools.Logger.Log(NetworkService.NetworkLog, "New Connection: {}", () => new object[] { connectionID });

            // Register this connection to receive updates on scene changes.
            if (networkSocket.IsHost)
            {
                ServiceLocator.SceneService.OnLoadScene += Host_OnLoadScene;
                Host_OnLoadScene(ServiceLocator.SceneService.CurrentLoaded);
            }
            else
            {
                ServiceLocator.SceneService.OnFinishedLoading += Client_OnFinishedLoading;
            }
        }

        public void Disconnect()
        {
            currentState = NetworkConnectionState.Disconnecting;
            ServiceLocator.SceneService.OnLoadScene -= Host_OnLoadScene;
            ServiceLocator.SceneService.OnFinishedLoading -= Client_OnFinishedLoading;

            if (IsLocalConnection)
                networkSocket.Close();
            DevTools.Logger.Log(NetworkService.NetworkLog, "Disconnect: {}", () => new object[] { connectionID });
        }

        public void SendData(int channelId, byte[] data, int dataSize, Func<string> dataSendType = null)
        {
            byte error;
            NetworkTransport.Send(networkSocket.SocketID, connectionID, channelId, data, dataSize, out error);

            if (dataSendType == null)
                DevTools.Logger.Log(NetworkService.NetworkOutgoingLog, "Sending {} {} bytes.", () => new object[] { connectionID, dataSize });
            else DevTools.Logger.Log(NetworkService.NetworkOutgoingLog, "Sending {} {} bytes for {}", () => new object[] { connectionID, dataSize, dataSendType() });

            if (error != 0)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "SendData: {}", () => new object[] { (NetworkError)error });
        }

        public void ReceiveData(int channelId, byte[] data, int dataSize)
        {
            if (!networkSocket.IsHost && !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Received data from a none local connection. This is not allowed.");
                return;
            }

            NetworkReader reader = new NetworkReader(data);
            ushort sz = reader.ReadUInt16();
            short msgType = reader.ReadInt16();

            DevTools.Logger.Log(NetworkService.NetworkIncomingLog, "Received {} bytes for {}", () => new object[] { dataSize, msgType });

            switch (msgType)
            {
                case MessageTypes.BRF_DebugTest:
                    Debug.Log("Received Debug Test");
                    break;
                case MessageTypes.BRF_Client_Receive_OnLoadScene:
                    Client_Receive_OnLoadScene(reader);
                    break;
                case MessageTypes.BRF_Host_Receive_OnFinishedLoading:
                    Host_Receive_OnFinishedLoading(reader);
                    break;
                case MessageTypes.BRF_Client_Receive_OnReady:
                    Client_Receive_OnReady();
                    break;
                case MessageTypes.BRF_Client_Receive_GameObject:
                    Client_Receive_GameObject(reader);
                    break;
                case MessageTypes.BRF_Client_Update_GameObject:
                    NoneOwner_Receive_Update_GameObject(reader);
                    break;
                case MessageTypes.BRF_Client_Ownership_GameObject:
                    Client_Receive_Ownership_GameObject(reader);
                    break;
                default:
                    Debug.AssertFormat(false, "wrong msgType {0}", msgType);
                    break;
            }
        }

        //
        // Level Load Events
        //

        // Called when the host starts to load a new scene.
        private void Host_OnLoadScene(Scenes.SceneLoadInfo sceneLoadInfo)
        {
            if (!networkSocket.IsHost || IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Host Method called on a client.");
                return;
            }

            currentState = NetworkConnectionState.Loading;
            NetworkWriter writer = networkSocket.Writer.Setup(networkSocket.ReliableSequencedChannel, MessageTypes.BRF_Client_Receive_OnLoadScene);
            writer.Write(ServiceLocator.PlatformService.Username); // Tell client what our name is.
            sceneLoadInfo.NetworkWrite(writer);
            networkSocket.Writer.Send(this, () => "Send Client SceneLoadInfo");
        }

        private void Client_Receive_OnLoadScene(NetworkReader reader)
        {
            if (networkSocket.IsHost || !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Client Method called on a host.");
                return;
            }

            currentState = NetworkConnectionState.Loading;
            friendlyName = reader.ReadString();
            Scenes.SceneLoadInfo loadInfo = new Scenes.SceneLoadInfo(reader);
            ServiceLocator.SceneService.LoadScene(loadInfo);
        }

        // Called when the client has finished loading the scene.
        private void Client_OnFinishedLoading(Scenes.SceneLoadInfo sceneLoadInfo)
        {
            if (networkSocket.IsHost || !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Client Method called on a host.");
                return;
            }

            // Tell host we have finished loading.
            currentState = NetworkConnectionState.Waiting;
            NetworkWriter writer = networkSocket.Writer.Setup(networkSocket.ReliableSequencedChannel, MessageTypes.BRF_Host_Receive_OnFinishedLoading);
            writer.Write(ServiceLocator.PlatformService.Username); // Tell host what our name is.
            networkSocket.Writer.Send(this, () => "Send host finished loading");
        }

        private void Host_Receive_OnFinishedLoading(NetworkReader reader)
        {
            if (!networkSocket.IsHost || IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Host Method called on a client.");
                return;
            }

            currentState = NetworkConnectionState.Waiting;
            friendlyName = reader.ReadString();

            if (ServiceLocator.SceneService.CurrentState == Scenes.SceneLoadingState.Loaded)
            {
                Host_OnReady();
            } else
            {
                ServiceLocator.SceneService.OnFinishedLoading += Host_OnFinishedLoading;
            }
        }

        // Used only when the client finishes loading before the host does.
        private void Host_OnFinishedLoading(Scenes.SceneLoadInfo obj)
        {
            ServiceLocator.SceneService.OnFinishedLoading -= Host_OnFinishedLoading;
            if (currentState == NetworkConnectionState.Waiting)
                Host_OnReady();
        }

        //
        // Ready Events
        //

        private void Host_OnReady()
        {
            if (!networkSocket.IsHost || IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Host Method called on a client.");
                return;
            }

            currentState = NetworkConnectionState.Ready;
            NetworkWriter writer = networkSocket.Writer.Setup(networkSocket.ReliableSequencedChannel, MessageTypes.BRF_Client_Receive_OnReady);
            networkSocket.Writer.Send(this, () => "Send client ready");
            OnReady(this);
        }

        private void Client_Receive_OnReady()
        {
            if (networkSocket.IsHost || !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Client Method called on a host.");
                return;
            }

            currentState = NetworkConnectionState.Ready;
            OnReady(this);
        }

        //
        // Network Game Object Events
        //

        private void Client_Receive_GameObject(NetworkReader reader)
        {
            if (networkSocket.IsHost || !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Client Method called on a host.");
                return;
            }

            Pool.PoolDefinition poolDefinition = ServiceLocator.SaveService.SavedObjectReferences.GetSavedObject<Pool.PoolDefinition>(reader.ReadInt16());
            NetworkGameObject networkObject = ServiceLocator.PoolService.SpawnDefinition<NetworkGameObject>(poolDefinition);
            networkObject.Client_ReceiveGameObject(reader);
        }

        private void NoneOwner_Receive_Update_GameObject(NetworkReader reader)
        {
            short networkID = reader.ReadInt16();
            NetworkGameObject networkObject = ServiceLocator.NetworkService.GetNetworkGameObject(networkID);
            if (networkObject != null)
                networkObject.Client_ReceiveGameObjectUpdate(reader);
            else
                DevTools.Logger.LogWarning(NetworkService.NetworkLog, "Received NetworkID {} which does not exist.", () => new object[] { networkID });
        }

        private void Client_Receive_Ownership_GameObject(NetworkReader reader)
        {
            if (networkSocket.IsHost || !IsLocalConnection)
            {
                DevTools.Logger.LogError(NetworkService.NetworkLog, "Client Method called on a host.");
                return;
            }

            short networkID = reader.ReadInt16();
            NetworkGameObject networkObject = ServiceLocator.NetworkService.GetNetworkGameObject(networkID);
            if (networkObject != null)
                networkObject.Client_ReceiveOwnershipChange(reader);
            else
                DevTools.Logger.LogWarning(NetworkService.NetworkLog, "Received NetworkID {} which does not exist.", () => new object[] { networkID });
        }
    }
}