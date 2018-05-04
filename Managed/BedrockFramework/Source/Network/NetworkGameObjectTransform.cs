/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
// TODO: Only update when necessary.
// TODO: Control whether this is an initial update or stream.
// TODO: Heavily compress Vector3.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

namespace BedrockFramework.Network
{
    [System.Serializable]
    public class NetworkGameObjectTransform : INetworkComponent
    {
        public bool enabled = true;

        [HideInInspector]
        public Transform observed;

        public int NumNetVars { get { return 1; } }

        public void WriteUpdatedNetVars(NetworkWriter toWrite, ref bool[] updatedVars, int currentPosition)
        {
            if (observed == null)
                return;

            updatedVars[currentPosition] = true;
            toWrite.Write(observed.position);
        }

        public void ReadUpdatedNetVars(NetworkReader reader)
        {
            if (observed == null)
                return;

            observed.position = reader.ReadVector3();
        }
    }
}