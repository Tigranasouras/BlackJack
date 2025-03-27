using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene
{
    public class SceneLoad : MonoBehaviour
    {
        public void Login()
        {
            SceneManager.LoadScene(2);
        }

        public void PlayGame()
        {
            SceneManager.LoadScene(3);
        }

        public void GameoverScene()
        {
            Invoke("Gameover", 2);
        }

        private void Gameover()
        {
            SceneManager.LoadScene(4);
        }
    }
}
