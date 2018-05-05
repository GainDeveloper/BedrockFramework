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
        public float minDistance = 0.05f;


        public int NumNetVars { get { return netVarsToUpdate.Length; } }

        private bool[] netVarsToUpdate = new bool[1];
        private Transform observed;
        private Vector3 lastSentPosition;

        public void Setup(Transform toObserve)
        {
            this.observed = toObserve;
        }

        // Calculate any specific netvars that need to be updated.
        public bool[] GetNetVarsToUpdate()
        {
            if (lastSentPosition == null || Vector3.Distance(lastSentPosition, observed.position) > minDistance)
                netVarsToUpdate[0] = true;

            return netVarsToUpdate;
        }

        public void WriteUpdatedNetVars(NetworkWriter toWrite, bool forceUpdate)
        {
            if (observed == null)
                return;

            if (forceUpdate || netVarsToUpdate[0])
            {
                toWrite.Write(observed.position);
                lastSentPosition = observed.position;
            }

            // Reset sent netvars to update.
            for (int i = 0; i < netVarsToUpdate.Length; i++)
                netVarsToUpdate[i] = false;
        }

        public void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool forceUpdate)
        {
            if (observed == null)
                return;

            if (forceUpdate || updatedNetVars[currentPosition])
                observed.position = reader.ReadVector3();
        }
    }
}