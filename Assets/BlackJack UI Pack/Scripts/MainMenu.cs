using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Menu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject PlayerPanel;
        [SerializeField] GameObject GiftPanel;
        [SerializeField] GameObject MessagePanel;
        [SerializeField] GameObject SettingsPanel;
        [SerializeField] GameObject BlackJackPanel;
        [SerializeField] GameObject GeneralSettingsPanel;

        public void playerInfo()
        {
            PlayerPanel.SetActive(true);
        }
        public void closePlayerInfo()
        {
            PlayerPanel.SetActive(false);
        }

        public void gift()
        {
            GiftPanel.SetActive(true);
        }
        public void giftClose()
        {
            GiftPanel.SetActive(false);
        }

        public void message()
        {
            MessagePanel.SetActive(true);
        }

        public void closeMessage()
        {
            MessagePanel.SetActive(false);
        }

        public void settings()
        {
            SettingsPanel.SetActive(true);
        }

        public void blackJackSettings()
        {
            BlackJackPanel.SetActive(true);
            GeneralSettingsPanel.SetActive(false);
        }
        public void generalSettings()
        {
            GeneralSettingsPanel.SetActive(true);
            BlackJackPanel.SetActive(false);
        }

        public void closeSettings()
        {
            SettingsPanel.SetActive(false);
        }
    }
}
