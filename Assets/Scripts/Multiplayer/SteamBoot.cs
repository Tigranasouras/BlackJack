using UnityEngine;
using Steamworks;

public class SteamBoot : MonoBehaviour
{
    void Awake()
    {
        try
        {
            if (!SteamAPI.Init())
            {
                Debug.LogError("SteamAPI.Init() failed.");
                return;
            }
            Debug.Log("Steam initialized as: " + SteamFriends.GetPersonaName());
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("Missing native Steam DLLs: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (SteamAPI.IsSteamRunning())
            SteamAPI.Shutdown();
    }

    void Update() => SteamAPI.RunCallbacks();
}
