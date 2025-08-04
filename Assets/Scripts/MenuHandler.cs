using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.VisualScripting.Member;


public class MenuHandler : MonoBehaviour
{
    public AudioSource source;

    public void goToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
