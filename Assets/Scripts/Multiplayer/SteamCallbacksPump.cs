using UnityEngine;
using Steamworks;

public class SteamCallbacksPump : MonoBehaviour
{
    private static SteamCallbacksPump _instance;
    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (SteamManager.Initialized) SteamAPI.RunCallbacks();
    }
}
