/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Representions a connection between two clients.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace BedrockFramework.Network
{
    public class NetworkConnection
    {
        int connectionID;
        NetworkSocket networkSocket;

        public int ConnectionID { get { return connectionID; } }
        public bool LocalConnection { get { return connectionID == this.networkSocket.LocalConnectionID; } }

        public NetworkConnection(NetworkSocket networkSocket, int connectionID)
        {
            this.connectionID = connectionID;
            this.networkSocket = networkSocket;
            DevTools.Logger.Log(NetworkService.NetworkLog, "New Connection: {}", () => new object[] { connectionID });
        }

        public void Disconnect()
        {
            if (LocalConnection)
                networkSocket.Close();
            DevTools.Logger.Log(NetworkService.NetworkLog, "Disconnect: {}", () => new object[] { connectionID });
        }

        public void SendData(int channelId, byte[] data, int dataSize)
        {
            byte error;
            NetworkTransport.Send(networkSocket.SocketID, connectionID, channelId, data, dataSize, out error);

            if (error != 0)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "SendData: {}", () => new object[] { (NetworkError)error });
        } 

        public void ReceiveData(int channelId, byte[] data, int dataSize)
        {
            if (!networkSocket.IsHost && !LocalConnection)
            {
                DevTools.Logger.Log(NetworkService.NetworkLog, "Received data from a none local connection. This is not allowed.");
                return;
            }

            Debug.Log(Encoding.ASCII.GetString(data));
        }
    }
}