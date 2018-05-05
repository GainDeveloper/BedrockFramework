/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
//TODO: Handle triggers, perhaps should be RPC calls.
//TODO: Initial setup will need to handle full state syncing (current state and state time). Might not be an issue for smaller animations. 
//TODO: Compress floats. Bools could also be put into one byte if we have a lot of them.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BedrockFramework.Network
{
    [System.Serializable]
    public class NetworkGameObjectAnimator : INetworkComponent
    {
        public bool enabled = true;

        public int NumNetVars { get { return observed.parameterCount; } }

        private bool[] netVarsToUpdate;
        private Animator observed;
        private AnimatorControllerParameter[] parameters;
        private object[] previousParameterValues;

        public void Setup(Animator toObserve)
        {
            this.observed = toObserve;
            parameters = observed.parameters;
            netVarsToUpdate = new bool[observed.parameterCount];
        }

        public void TakenOwnership() { }
        public void LostOwnership() { }


        // Calculate any specific netvars that need to be updated.
        public bool[] GetNetVarsToUpdate()
        {
            if (observed == null || !enabled)
                return netVarsToUpdate;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (previousParameterValues == null || previousParameterValues.Length == 0)
                {
                    netVarsToUpdate[i] = true;
                } else
                {
                    switch (parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            if ((bool)previousParameterValues[i] != observed.GetBool(parameters[i].nameHash))
                                netVarsToUpdate[i] = true;
                            break;
                        case AnimatorControllerParameterType.Float:
                            if ((float)previousParameterValues[i] != observed.GetFloat(parameters[i].nameHash))
                                netVarsToUpdate[i] = true;
                            break;
                        case AnimatorControllerParameterType.Int:
                            if ((int)previousParameterValues[i] != observed.GetInteger(parameters[i].nameHash))
                                netVarsToUpdate[i] = true;
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                }
            }

            //Debug.Log(string.Join(", ", netVarsToUpdate.Select(x => x.ToString()).ToArray()));
            return netVarsToUpdate;
        }

        public void WriteUpdatedNetVars(NetworkWriter toWrite, bool force)
        {
            if (observed == null)
                return;

            if (previousParameterValues == null)
                previousParameterValues = new object[observed.parameterCount];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (force || netVarsToUpdate[i])
                {
                    switch (parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            bool boolValue = observed.GetBool(parameters[i].nameHash);
                            previousParameterValues[i] = boolValue;
                            toWrite.Write(boolValue);
                            break;
                        case AnimatorControllerParameterType.Float:
                            float floatValue = observed.GetFloat(parameters[i].nameHash);
                            previousParameterValues[i] = floatValue;
                            toWrite.Write(floatValue);
                            break;
                        case AnimatorControllerParameterType.Int:
                            int intValue = observed.GetInteger(parameters[i].nameHash);
                            previousParameterValues[i] = intValue;
                            toWrite.Write(intValue);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                }
            }

            // Reset sent netvars to update.
            for (int i = 0; i < netVarsToUpdate.Length; i++)
                netVarsToUpdate[i] = false;
        }

        public void ReadUpdatedNetVars(NetworkReader reader, bool[] updatedNetVars, int currentPosition, bool force, float sendRate)
        {
            if (observed == null) //We need to be reading regardless of whether we are observed/ enabled.
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (force || updatedNetVars[currentPosition + i])
                {
                    switch (parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            observed.SetBool(parameters[i].nameHash, reader.ReadBoolean());
                            break;
                        case AnimatorControllerParameterType.Float:
                            observed.SetFloat(parameters[i].nameHash, reader.ReadSingle());
                            break;
                        case AnimatorControllerParameterType.Int:
                            observed.SetInteger(parameters[i].nameHash, reader.ReadInt32());
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                }
            }
        }
    }
}