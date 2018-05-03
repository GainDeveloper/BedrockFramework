/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Receives all data from the host.
Stores list of connections.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;
using BedrockFramework.Pool;

namespace BedrockFramework.Network
{
    [HideMonoScript, AddComponentMenu("BedrockFramework/Network GameObject")]
    public class NetworkGameObject : MonoBehaviour, IPool
    {
        [ReadOnly, ShowInInspector]
        private PoolDefinition poolDefinition;
        [ReadOnly, ShowInInspector]
        private short networkID = 0;

        // Pool
        PoolDefinition IPool.PoolDefinition { set { poolDefinition = value; } }
        void IPool.OnDeSpawn()
        {
            ServiceLocator.NetworkService.OnNetworkConnectionReady -= OnNetworkConnectionReady;

        }

        void IPool.OnSpawn()
        {
            if (ServiceLocator.NetworkService.IsHost)
            {
                if (networkID == 0)
                    networkID = ServiceLocator.NetworkService.UniqueNetworkID;

                // Tell all active connections about our creation.
                if (ServiceLocator.NetworkService.IsActive)
                {
                    foreach (NetworkConnection active in ServiceLocator.NetworkService.ActiveSocket.ActiveConnections())
                    {
                        Host_SendGameObject(active);
                    }
                }

                ServiceLocator.NetworkService.OnNetworkConnectionReady += OnNetworkConnectionReady;
            }
        }

        private void OnNetworkConnectionReady(NetworkConnection readyConnection)
        {
            Host_SendGameObject(readyConnection);
        }

        void Host_SendGameObject(NetworkConnection receiver)
        {
            if (receiver.CurrentState != NetworkConnectionState.Ready)
                return;

            NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(receiver, ServiceLocator.NetworkService.ActiveSocket.ReliableChannel, MessageTypes.BRF_Client_Receive_GameObject);
            writer.Write(ServiceLocator.SaveService.SavedObjectReferences.GetSavedObjectID(poolDefinition));
            writer.Write(networkID);

            //TODO: Write out everything we want to send as initial data.

            ServiceLocator.NetworkService.ActiveSocket.Writer.Send(() => "NetworkGameObject Init");
        }

        public void Client_ReceiveGameObject(NetworkReader reader)
        {
            networkID = reader.ReadInt16();
        }
    }
}