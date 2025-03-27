using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Loading
{
    public class LoadingBar : MonoBehaviour
    {
        public float loadingSpeed = 0.5f;
        private Slider slider;
        private float targetProgress = 1f;
        public Text progressText;

        void Start()
        {
            slider = GetComponent<Slider>();
            progressText.text = "0%";
        }

        void Update()
        {
            if (slider.value < targetProgress)
            {
                slider.value += loadingSpeed * Time.deltaTime;
                int progressPercentage = Mathf.FloorToInt(slider.value * 100);
                progressText.text = progressPercentage + "%";
            }
            if (slider.value >= targetProgress)
            {
                slider.value = targetProgress;
                progressText.text = "100%";


                SceneManager.LoadScene(1);
            }


        }
    }
}

