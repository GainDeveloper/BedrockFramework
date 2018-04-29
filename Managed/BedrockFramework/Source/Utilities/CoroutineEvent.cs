using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BedrockFramework.Utilities
{
    public class CoroutineEvent
    {
        public List<Coroutine> coroutines = new List<Coroutine>();

        public IEnumerator WaitForCoroutines()
        {
            foreach (Coroutine coroutine in coroutines)
                yield return coroutine;
        }
    }
}