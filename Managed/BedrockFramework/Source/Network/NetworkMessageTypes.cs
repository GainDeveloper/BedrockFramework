/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Quick lookup for different message types.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

namespace BedrockFramework.Network
{
    public class MessageTypes
    {
        public const short BRF_Client_Receive_OnLoadScene = MsgType.Highest + 1;
        public const short BRF_Host_Receive_OnFinishedLoading = MsgType.Highest + 2;
        public const short BRF_Client_Receive_OnReady = MsgType.Highest + 3;
        public const short BRF_DebugTest = MsgType.Highest + 4;
        public const short BRF_Client_Receive_GameObject = MsgType.Highest + 5;
        public const short BRF_Client_Update_GameObject = MsgType.Highest + 6;
    }
}