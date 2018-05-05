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
    public class NetworkGameObjectRigidbody : INetworkComponent
    {
        public bool enabled = true;
        public float minVelocityDiff = 0.05f;


        public int NumNetVars { get { return netVarsToUpdate.Length; } }

        private bool[] netVarsToUpdate = new bool[1];
        private Rigidbody observed;
        private Vector3 lastSentVelocity;

        public void Setup(Rigidbody toObserve)
        {
            this.observed = toObserve;
        }

        // Calculate any specific netvars that need to be updated.
        public bool[] GetNetVarsToUpdate()
        {
            if (observed == null)
                return netVarsToUpdate;

            if (lastSentVelocity == null || Vector3.Distance(lastSentVelocity, observed.velocity) > minVelocityDiff)
                netVarsToUpdate[0] = true;

            return netVarsToUpdate;
        }

        public void WriteUpdatedNetVars(NetworkWriter toWrite, bool force)
        {
            if (observed == null)
                return;

            if (force || netVarsToUpdate[0])
            {
                toWrite.Write(observed.velocity);
                lastSentVelocity = observed.velocity;
            }

            // Reset sent netvars to update.
            for (int i = 0; i < netVarsToUpdate.Length; i++)
                netVarsToUpdate[i] = false;
        }

        public void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool force)
        {
            if (observed == null)
                return;

            if (force || updatedNetVars[currentPosition])
                observed.velocity = reader.ReadVector3();
        }
    }
}