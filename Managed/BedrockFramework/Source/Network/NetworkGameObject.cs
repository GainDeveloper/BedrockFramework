/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Host always sends initialisation data.
Receives all data from current owner.
Host can change ownership at will.
TODO: Create an interface and try and generalise most of what is here so we can have Networked Components.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using BedrockFramework.Pool;
using BedrockFramework.Utilities;

namespace BedrockFramework.Network
{
    public interface INetworkComponent
    {
        int NumNetVars { get; }
        bool[] GetNetVarsToUpdate();
        void OnNetworkInitialized();
        void WriteUpdatedNetVars(NetworkWriter toWrite, bool forceAll);
        void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool forceAll, float sendRate);
        void TakenOwnership(bool hasReceivedNewUpdates);
        void LostOwnership(bool hasReceivedNewUpdates);
    }

    [HideMonoScript, AddComponentMenu("BedrockFramework/Network GameObject")]
    public class NetworkGameObject : MonoBehaviour, IPool
    {
        public byte updatesPerSecond  = 3;
        public NetworkGameObjectTransform networkTransform = new NetworkGameObjectTransform();
        public NetworkGameObjectRigidbody networkRigidbody = new NetworkGameObjectRigidbody();
        public NetworkGameObjectAnimator networkAnimator = new NetworkGameObjectAnimator();

        [ReadOnly, ShowInInspector]
        private PoolDefinition poolDefinition;
        [ReadOnly, ShowInInspector]
        private short networkID = 0;
        [ReadOnly, ShowInInspector, HorizontalGroup("Ownership")]
        private NetworkConnection owner = null;
        private bool ownerHasBeenSet = false;

        private Coroutine activeLoop;
        private List<INetworkComponent> activeNetworkComponents = new List<INetworkComponent>();
        private float lastSentTime, lastReceivedTime;

        public short NetworkID { get { return networkID; } }
        public bool OwnedByUs { get { return owner == null; } }

        // Pool
        PoolDefinition IPool.PoolDefinition { set { poolDefinition = value; } }

        void IPool.OnSpawn()
        {
            ownerHasBeenSet = false;
            activeNetworkComponents = GetComponents<INetworkComponent>().ToList();

            if (networkRigidbody.enabled)
            {
                networkRigidbody.Setup(gameObject.GetComponent<Rigidbody>());
                activeNetworkComponents.Add((INetworkComponent)networkRigidbody);
            }
            if (networkAnimator.enabled)
            {
                networkAnimator.Setup(gameObject.GetComponent<Animator>());
                activeNetworkComponents.Add((INetworkComponent)networkAnimator);
            }

            networkTransform.Setup(gameObject.transform);
            if (networkTransform.enabled)
                activeNetworkComponents.Add((INetworkComponent)networkTransform);

            ServiceLocator.NetworkService.OnBecomeHost += HostGameObject;
            if (ServiceLocator.NetworkService.IsHost)
                HostGameObject();

            ServiceLocator.NetworkService.OnNetworkConnectionDisconnected += OnNetworkConnectionDisconnected;
        }

        void IPool.OnDeSpawn()
        {
            ServiceLocator.NetworkService.OnNetworkConnectionDisconnected -= OnNetworkConnectionDisconnected;
            ServiceLocator.NetworkService.OnBecomeHost -= HostGameObject;
            NetworkCleanup();
        }

        private void HostGameObject()
        {
            if (!ServiceLocator.NetworkService.IsHost)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "None host is trying to host a network game object!");

            if (networkID == 0)
                networkID = ServiceLocator.NetworkService.UniqueNetworkID;

            ServiceLocator.NetworkService.AddNetworkGameObject(this);
            StartOwning();

            if (ServiceLocator.NetworkService.IsActive)
            {
                // Tell all active connections about our creation.
                Host_SendGameObject(ServiceLocator.NetworkService.ActiveSocket.ActiveConnections().Where(x => x.CurrentState == NetworkConnectionState.Ready).ToArray());
                ServiceLocator.NetworkService.OnNetworkConnectionReady += OnNetworkConnectionReady;
                ServiceLocator.NetworkService.OnStop += NetworkService_OnStop;
            }
        }

        private void NetworkService_OnStop()
        {
            StartOwning();
            NetworkCleanup();
        }

        private void NetworkCleanup()
        {
            ServiceLocator.NetworkService.RemoveNetworkGameObject(this);
            ServiceLocator.NetworkService.OnNetworkConnectionReady -= OnNetworkConnectionReady;
            ServiceLocator.NetworkService.OnStop -= NetworkService_OnStop;
        }

        private void OnNetworkConnectionReady(NetworkConnection readyConnection)
        {
            if (!ServiceLocator.NetworkService.IsHost)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "None host is trying to handle a connection ready network game object request!");

            Host_SendGameObject(new NetworkConnection[] { readyConnection });
        }

        private void OnNetworkConnectionDisconnected(NetworkConnection disconnected)
        {
            if (owner == disconnected)
                SetOwner(null);
        }

        //
        // Initialization
        //

        void Host_SendGameObject(NetworkConnection[] receivers)
        {
            if (receivers.Length == 0)
            {
                return;
            }

            NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(ServiceLocator.NetworkService.ActiveSocket.ReliableSequencedChannel, MessageTypes.BRF_Client_Receive_GameObject);
            writer.Write(ServiceLocator.SaveService.SavedObjectReferences.GetSavedObjectID(poolDefinition));
            writer.Write(networkID);

            // Send initial data for active components.
            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].WriteUpdatedNetVars(writer, true);
            }

            for (int i = 0; i < receivers.Length; i++)
                ServiceLocator.NetworkService.ActiveSocket.Writer.Send(receivers[i], () => "NetworkGameObject Init");

            // Let other components know we have sent the gameobject to the network.
            for (int i = 0; i < activeNetworkComponents.Count; i++)
                activeNetworkComponents[i].OnNetworkInitialized();
        }

        public void Client_ReceiveGameObject(NetworkConnection from, NetworkReader reader)
        {
            networkID = reader.ReadInt16();
            lastReceivedTime = Time.time;
            SetOwner(from);
            ServiceLocator.NetworkService.AddNetworkGameObject(this);

            int currentPosition = 0;
            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].ReadUpdatedNetVars(reader, null, currentPosition, true, 0);
                currentPosition += activeNetworkComponents[i].NumNetVars;
            }
        }

        //
        // Host Update Loop
        //

        int NumNetVars { get {
                int i = 0;
                foreach (INetworkComponent comp in activeNetworkComponents)
                    i += comp.NumNetVars;
                return i;
            } }

        // Collects all NetVars in active network components and send those that have been updated.
        IEnumerator UpdateLoop()
        {
            bool[] updatedNetVars = new bool[NumNetVars];
            while (ServiceLocator.NetworkService.IsActive)
            {
                yield return new WaitForSecondsRealtime(1f / updatesPerSecond);

                // Get updated netvars
                RefreshUpdatedNetVars(ref updatedNetVars);

                // Check if any NetVars were updated.
                if (ServiceLocator.NetworkService.IsActive && updatedNetVars.ArrayContainsValue(true) && ServiceLocator.NetworkService.ActiveSocket.NumActiveConnections > 0)
                {
                    WriteAndSendNetVars(updatedNetVars);
                }
            }
        }

        private void WriteAndSendNetVars(bool[] updatedNetVars)
        {
            // Write updated net vars.
            NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(ServiceLocator.NetworkService.ActiveSocket.UnreliableChannel, MessageTypes.BRF_Client_Update_GameObject);
            writer.Write(networkID);
            writer.Write(updatedNetVars.ToByteArray(), NumNetVars.BoolArraySizeToByteArraySize());

            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].WriteUpdatedNetVars(writer, false);
            }

            // Send NetVars
            foreach (NetworkConnection active in ServiceLocator.NetworkService.ActiveSocket.ActiveConnections())
            {
                if (active.CurrentState != NetworkConnectionState.Ready)
                    continue;

                ServiceLocator.NetworkService.ActiveSocket.Writer.Send(active, () => "NetworkGameObject Update");
            }

            lastSentTime = Time.time;
        }

        /// <summary>
        /// Updates the list of bools with what vars want to be synced.
        /// </summary>
        /// <param name="updatedNetVars"></param>
        private void RefreshUpdatedNetVars(ref bool[] updatedNetVars)
        {
            int currentPosition = 0;
            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].GetNetVarsToUpdate().CopyTo(updatedNetVars, currentPosition);
                currentPosition += activeNetworkComponents[i].NumNetVars;
            }
        }

        //
        // Client Update Loop
        //

        public void Client_ReceiveGameObjectUpdate(NetworkReader reader)
        {
            lastReceivedTime = Time.time;

            // Read the bool array with what has been updated.
            bool[] updatedNetVars = reader.ReadBytes(NumNetVars.BoolArraySizeToByteArraySize()).ToBoolArray();

            //Debug.Log(string.Join(", ", updatedNetVars.Select(x => x.ToString()).ToArray()));

            int currentPosition = 0;
            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].ReadUpdatedNetVars(reader, updatedNetVars, currentPosition, false, 1f / updatesPerSecond);
                currentPosition += activeNetworkComponents[i].NumNetVars;
            }
        }

        //
        // Ownership
        //

        private void StartOwning()
        {
            DevTools.Logger.Log(NetworkService.NetworkLog, "Taking ownership of {}.", () => new object[] { gameObject.name });
            bool isDifferentOwner = owner != null;

            owner = null;

            if (isDifferentOwner || !ownerHasBeenSet)
            {
                for (int i = 0; i < activeNetworkComponents.Count; i++)
                    activeNetworkComponents[i].TakenOwnership(lastReceivedTime > lastSentTime);
            }

            if (ServiceLocator.NetworkService.IsActive)
            {
                if (activeLoop != null)
                    Debug.LogAssertion("Starting update coroutine when one already exists!");
                activeLoop = StartCoroutine(UpdateLoop());
            }

            ownerHasBeenSet = true;
        }

        private void StopOwning(NetworkConnection newOwner)
        {
            DevTools.Logger.Log(NetworkService.NetworkLog, "Discarding ownership of {}. New Owner : {}", () => new object[] {gameObject.name, newOwner != null ? newOwner.ConnectionID : 0});
            bool isDifferentOwner = newOwner != owner;

            if (activeLoop != null)
            {
                StopCoroutine(activeLoop);
                activeLoop = null;
            }

            owner = newOwner;

            if (isDifferentOwner || !ownerHasBeenSet)
            {
                for (int i = 0; i < activeNetworkComponents.Count; i++)
                    activeNetworkComponents[i].LostOwnership(lastSentTime > lastReceivedTime);
            }

            ownerHasBeenSet = true;
        }

        public void SetOwner(NetworkConnection newOwner)
        {
            if (owner == newOwner)
                return;

            if (ServiceLocator.NetworkService.IsHost)
            {
                if (newOwner == null)
                {
                    Host_SendOwnership(owner, false);
                    StartOwning();
                }
                else if (owner == null)
                {
                    StopOwning(newOwner);
                    Host_SendOwnership(newOwner, true);
                }
            }
            else
            {
                if (newOwner == null)
                    StartOwning();
                else if (owner == null)
                    StopOwning(newOwner);
            }
        }

        [Button, HorizontalGroup("Ownership")]
        private void BecomeOwner()
        {
            if (ServiceLocator.NetworkService.IsHost)
            {
                SetOwner(null);
            }
        }

        public void Host_SendOwnership(NetworkConnection connection, bool takeOwnership)
        {
            if (!ServiceLocator.NetworkService.IsHost)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "None host is trying to send ownership of a NetworkGameObject!");

            if (connection.CurrentState == NetworkConnectionState.Disconnecting)
                return;

            NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(ServiceLocator.NetworkService.ActiveSocket.ReliableSequencedChannel, MessageTypes.BRF_Client_Ownership_GameObject);
            writer.Write(networkID);
            writer.Write(takeOwnership);
            ServiceLocator.NetworkService.ActiveSocket.Writer.Send(connection, () => "NetworkGameObject Ownership");
        }

        public void Client_ReceiveOwnershipChange(NetworkConnection receivedFrom, NetworkReader reader)
        {
            bool takeOwnership = reader.ReadBoolean();

            if (takeOwnership)
                SetOwner(null);
            else
                SetOwner(receivedFrom);
        }
    }
}