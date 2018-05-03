/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Ensures the network writer with the correct data.
Allows for easy reuse of the writer.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

namespace BedrockFramework.Network
{
    public class NetworkWriterWrapper
    {
        NetworkSocket socket;
        NetworkWriter writer;

        public NetworkWriterWrapper(NetworkSocket socket)
        {
            this.socket = socket;
            this.writer = new NetworkWriter();
        }

        NetworkConnection recipient;
        int channelID;

        //TODO: Should extend setup with varients for specific types of messages (i.e messages between network objects)
        public NetworkWriter Setup(NetworkConnection recipient, int channelID, short messageType)
        {
            this.recipient = recipient;
            this.channelID = channelID;
            writer.StartMessage(messageType);

            return writer;
        }

        public void Send(Func<string> dataSendType = null)
        {
            writer.FinishMessage();

            if (dataSendType == null)
                socket.SendData(recipient, channelID, writer.ToArray(), writer.Position, null);
            else
                socket.SendData(recipient, channelID, writer.ToArray(), writer.Position, dataSendType);
        }
    }
}