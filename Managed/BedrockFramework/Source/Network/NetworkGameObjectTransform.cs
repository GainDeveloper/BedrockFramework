/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
// TODO: Handle teleporting and other 'large' position updates.
// TODO: Interpolation could work with physics rather than bypassing it. Perhaps it's just a case of having faster interpolation.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using BedrockFramework.Utilities;
using DG.Tweening;

namespace BedrockFramework.Network
{
    [System.Serializable]
    public class NetworkGameObjectTransform : INetworkComponent
    {
        public bool enabled = true;
        public float minDistance = 0.05f;
        public float maxDistance = 1.5f;
        public float minAngle = 3f;
        public float interpolationScale = 0.2f;

        public int NumNetVars { get { return netVarsToUpdate.Length; } }

        private bool[] netVarsToUpdate = new bool[2];
        private Transform observed;
        private Vector3 lastSentPosition, lastReceivedPosition;
        private float lastSentYAngle = -1000, lastReceivedAngle;

        public void Setup(Transform toObserve)
        {
            this.observed = toObserve;
        }

        public void TakenOwnership()
        {
            lastSentPosition = lastReceivedPosition;
            lastSentYAngle = lastReceivedAngle;
        }

        public void LostOwnership()
        {
            lastReceivedPosition = lastSentPosition;
            lastReceivedAngle = lastSentYAngle;
        }

        // Calculate any specific netvars that need to be updated.
        public bool[] GetNetVarsToUpdate()
        {
            if (observed == null || !enabled)
                return netVarsToUpdate;

            if (lastSentPosition == null || Vector3.Distance(lastSentPosition, observed.position) > minDistance)
                netVarsToUpdate[0] = true;

            if (lastSentYAngle == -1000 || Mathf.Abs(lastSentYAngle - observed.rotation.eulerAngles.y) > minAngle)
                netVarsToUpdate[1] = true;

            return netVarsToUpdate;
        }

        public void WriteUpdatedNetVars(NetworkWriter toWrite, bool forceUpdate)
        {
            if (observed == null)
                return;

            if (forceUpdate || netVarsToUpdate[0])
            {
                // For initilisation we send the full position.
                if (forceUpdate)
                {
                    toWrite.Write(observed.position);
                    lastSentPosition = observed.position;
                } else
                {
                    // Create a derivative that max's the movement.
                    Vector3 diff = observed.position - lastSentPosition;
                    byte[] diffBytes = (Vector3.ClampMagnitude(diff, maxDistance) / maxDistance).Vector3ToByteArray();
                    toWrite.Write(diffBytes, 3);
                    diffBytes.ByteArrayToVector3(out diff);
                    lastSentPosition += diff * maxDistance;
                }
            }

            if (forceUpdate || netVarsToUpdate[1])
            {
                byte angle = (observed.eulerAngles.y.Wrap(0, 360) / 360).ZeroOneToByte();
                toWrite.Write(angle);
                lastSentYAngle = observed.eulerAngles.y;
            }

            // Reset sent netvars to update.
            for (int i = 0; i < netVarsToUpdate.Length; i++)
                netVarsToUpdate[i] = false;
        }

        public void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool forceUpdate, float sendRate)
        {
            if (observed == null) //We need to be reading regardless of whether we are observed/ enabled.
                return;

            if (forceUpdate || updatedNetVars[currentPosition])
            {
                if (forceUpdate)
                {
                    observed.position = reader.ReadVector3();
                    lastReceivedPosition = observed.position;
                } else
                {
                    lastReceivedPosition += reader.ReadBytes(3).ByteArrayToVector3() * maxDistance;
                    observed.DOMove(lastReceivedPosition, sendRate * interpolationScale).SetEase(Ease.Linear);
                }
            }

            if (forceUpdate || updatedNetVars[currentPosition + 1])
            {
                lastReceivedAngle = reader.ReadByte().ZeroOneToFloat() * 360;
                if (forceUpdate)
                    observed.eulerAngles = new Vector3(0, lastReceivedAngle, 0);
                else
                    observed.DORotate(new Vector3(0, lastReceivedAngle, 0), sendRate * interpolationScale);
            }
        }
    }
}