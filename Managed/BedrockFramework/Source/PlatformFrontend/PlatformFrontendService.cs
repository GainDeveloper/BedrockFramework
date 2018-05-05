/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Scene Service. Handles loading scenes, subscenes and sending out the correct messaging.
********************************************************/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using BedrockFramework.Utilities;
using ProtoBuf;

namespace BedrockFramework.PlatformFrontend
{
    public interface IPlatformFrontendService
    {
        string Username { get; }
        void Close();
    }

    public class NullPlatformFrontendService : IPlatformFrontendService
    {
        public string Username { get { return "Unnamed"; } }
        public void Close() { }
    }
}