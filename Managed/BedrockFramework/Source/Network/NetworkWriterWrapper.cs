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

        private bool finished = false;

        public NetworkWriterWrapper(NetworkSocket socket)
        {
            this.socket = socket;
            this.writer = new NetworkWriter();
        }

        int channelID;

        public NetworkWriter Setup(int channelID, short messageType)
        {
            finished = false;

            this.channelID = channelID;
            writer.StartMessage(messageType);

            return writer;
        }

        public void Send(NetworkConnection recipient, Func<string> dataSendType = null)
        {
            if (!finished)
            {
                writer.FinishMessage();
                finished = true;
            }

            if (dataSendType == null)
                socket.SendData(recipient, channelID, writer.ToArray(), writer.Position, null);
            else
                socket.SendData(recipient, channelID, writer.ToArray(), writer.Position, dataSendType);
        }
    }
}