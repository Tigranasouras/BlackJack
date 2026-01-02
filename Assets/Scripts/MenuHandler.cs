using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.VisualScripting.Member;
using Steamworks;


public class MenuHandler : MonoBehaviour
{
    public AudioSource source;

    private void LeaveLobbyIfAny()
    {
        var bridge = FindFirstObjectByType<LobbyBridge>();
        if (bridge != null && bridge.HasLobby && SteamManager.Initialized)
        {
            SteamMatchmaking.LeaveLobby(bridge.LobbyId);
            bridge.Clear();
        }
    }

    public void goToMenu()
    {
        LeaveLobbyIfAny();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    public void goToLobby()
    {
        LeaveLobbyIfAny();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
    public void goToGameNoMusic()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("SinglePlayer");
    }

    public void goToGame2NoMusic()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MultiPlayer");
    }

    private IEnumerator WaitForSoundAndTransition(string sceneName)
    {

        source.Play();
        yield return new WaitForSeconds(source.clip.length);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    public void goToGame()
    {
        StartCoroutine(WaitForSoundAndTransition("SinglePlayer"));
    }

    public void restartGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void quitGame()
    {
        Application.Quit();
    }

}
