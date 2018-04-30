/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Decides how we react to different network connections.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace BedrockFramework.Network
{
    public class NetworkConnection
    {
        int connectionID;

        public NetworkConnection(int connectionID)
        {
            this.connectionID = connectionID;
            DevTools.Logger.Log(NetworkService.NetworkLog, "New Connection: {}", () => new object[] { connectionID });
        }

        public void Disconnect()
        {
            DevTools.Logger.Log(NetworkService.NetworkLog, "Disconnect: {}", () => new object[] { connectionID });
        }
    }
}