using UnityEngine;
using Steamworks;

public class SteamBoot : MonoBehaviour
{

    private static SteamBoot _instance;

    void Awake()
    {
        if (_instance) { Destroy(gameObject); return; }
        _instance = this; DontDestroyOnLoad(gameObject);

        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized. Ensure steam_appid.txt and run under Steam/ overlay.");
        }
        else
        {
            Debug.Log($"Steam initialized as: {SteamFriends.GetPersonaName()}");
        }

    }

    private void Update()
    {
        if (SteamManager.Initialized) SteamAPI.RunCallbacks();
    }
}
