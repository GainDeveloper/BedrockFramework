/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
// TODO: Will need to handle faster velocities.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using BedrockFramework.Utilities;
using System.Collections.Generic;
using System;

namespace BedrockFramework.Network
{
    [System.Serializable]
    public class NetworkGameObjectRigidbody : INetworkComponent
    {
        public bool enabled = true;
        public float minVelocityDiff = 0.15f, maxVelocity = 10f;


        public int NumNetVars { get { return netVarsToUpdate.Length; } }

        private bool[] netVarsToUpdate = new bool[1];
        private Rigidbody observed;
        private Vector3 lastSentVelocity, lastReceivedVelocity;

        public void Setup(Rigidbody toObserve)
        {
            this.observed = toObserve;
        }

        public void TakenOwnership()
        {
            lastSentVelocity = lastReceivedVelocity;
        }

        public void LostOwnership()
        {
            lastReceivedVelocity = lastSentVelocity;
        }

        // Calculate any specific netvars that need to be updated.
        public bool[] GetNetVarsToUpdate()
        {
            if (observed == null || !enabled)
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
                Vector3 diff = observed.velocity - lastSentVelocity;
                byte[] diffBytes = (Vector3.ClampMagnitude(diff, maxVelocity) / maxVelocity).Vector3ToByteArray();
                toWrite.Write(diffBytes, 3);
                diffBytes.ByteArrayToVector3(out diff);
                lastSentVelocity += diff * maxVelocity;
            }

            // Reset sent netvars to update.
            for (int i = 0; i < netVarsToUpdate.Length; i++)
                netVarsToUpdate[i] = false;
        }

        public void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool force, float sendRate)
        {
            if (observed == null) //We need to be reading regardless of whether we are observed/ enabled.
                return;

            if (force || updatedNetVars[currentPosition])
            {
                if (force)
                {
                    observed.velocity = reader.ReadBytes(3).ByteArrayToVector3() * maxVelocity;
                    lastReceivedVelocity = observed.velocity;
                }
                else
                {
                    lastReceivedVelocity += reader.ReadBytes(3).ByteArrayToVector3() * maxVelocity;
                    observed.velocity = lastReceivedVelocity;
                }
            }
        }
    }
}