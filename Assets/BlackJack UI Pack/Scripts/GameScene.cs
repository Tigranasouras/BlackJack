using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameScene : MonoBehaviour
    {
        [SerializeField] GameObject AutomatchingPanel;
        [SerializeField] GameObject SittingPanel;
        [SerializeField] GameObject BettingPanel;
        [SerializeField] Text MoneyTxtOne;
        [SerializeField] Text MoneyTxtTwo;
        [SerializeField] GameObject ChipsComponent;
        [SerializeField] GameObject BettingButton;
        [SerializeField] GameObject CardPanel;
        [SerializeField] GameObject LoseScreen;

        private void Start()
        {
            Invoke("closeAutoMatching", 2);
        }

        void closeAutoMatching()
        {
            AutomatchingPanel.SetActive(false);
            SittingPanel.SetActive(true);
        }

        public void closeSittingPanel()
        {
            SittingPanel.SetActive(false);
            BettingPanel.SetActive(true);
            MoneyTxtOne.text = "10K";
            MoneyTxtTwo.text = "10K";
        }

        public void closeChips()
        {
            ChipsComponent.SetActive(false);
            BettingButton.SetActive(true);
        }

        public void closeBettingPanel()
        {
            BettingPanel.SetActive(false);
            CardPanel.SetActive(true);
        }

        public void LoseGame()
        {
            LoseScreen.SetActive(true);
        }
    }
}
