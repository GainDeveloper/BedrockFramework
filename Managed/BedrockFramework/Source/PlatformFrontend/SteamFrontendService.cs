/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

SteamFrontend Service, uses https://github.com/Facepunch/Facepunch.Steamworks
********************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using BedrockFramework.Utilities;
using Facepunch.Steamworks;

namespace BedrockFramework.PlatformFrontend
{
    public class SteamFrontendService : Service, IPlatformFrontendService
    {
        const string PlatformFrontendServiceLog = "Platform";
        const uint AppId = 480;

        private Facepunch.Steamworks.Client client;

        public string Username
        {
            get
            {
                if (client != null)
                    return client.Username;
                else return "Player";
            }
        }

        public SteamFrontendService(MonoBehaviour owner): base(owner)
        {
            Startup();
            DevTools.DebugMenu.AddDebugStats("Platform Stats", SteamFrontendStats);
        }

        IEnumerable<string> SteamFrontendStats()
        {
            yield return "Username: " + Username;
        }

        public void Close()
        {
            if (client != null)
                client.Dispose();
        }

        void Startup()
        {
            Facepunch.Steamworks.Config.ForUnity(Application.platform.ToString());

            try
            {
                System.IO.File.WriteAllText("steam_appid.txt", AppId.ToString());
            }
            catch (System.Exception e)
            {
                DevTools.Logger.LogError(PlatformFrontendServiceLog, "Couldn't write steam_appid.txt: {}", () => new object[] { e.Message});
            }

            // Create the client
            client = new Facepunch.Steamworks.Client(AppId);

            if (!client.IsValid)
            {
                client = null;
                DevTools.Logger.LogError(PlatformFrontendServiceLog, "Couldn't initialize Steam");
                return;
            }

            DevTools.Logger.Log(PlatformFrontendServiceLog, "Steam Initialized: {} / {}", () => new object[] { client.Username, client.SteamId });
        }
    }
}