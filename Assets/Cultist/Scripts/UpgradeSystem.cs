using UnityEngine;
using UnityEngine.UI;

public class UpgradeSystem : MonoBehaviour
{
    public int upgradeCost = 10;  // Cultists needed to get an upgrade.
    public Button upgradeButton;  // A UI Button to trigger an upgrade.
    public Text upgradeInfoText;  // Information on the upgrade.

    // Example upgrade parameters:
    public int additionalClickDamage = 5;
    // (You can add more fields if you decide to upgrade enemy parameters, etc.)

    private void Start()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(AttemptUpgrade);

        if (upgradeInfoText != null)
            upgradeInfoText.text = "Spend " + upgradeCost + " cultists to add " + additionalClickDamage + " damage.";
    }

    private void AttemptUpgrade()
    {
        if (CultManager.Instance.SpendCultists(upgradeCost))
        {
            // Apply upgrade: for instance, increase the global click damage or pass upgrade info to next enemy.
            ClickHandler clickHandler = FindObjectOfType<ClickHandler>();
            if (clickHandler != null)
            {
                clickHandler.clickDamage += additionalClickDamage;
            }
            // Optionally update the UI to reflect the upgrade.
            if (upgradeInfoText != null)
                upgradeInfoText.text = "Upgrade applied! New click damage: " + clickHandler.clickDamage;

            // After upgrade, proceed to combat for next level.
            GameManagers.Instance.LoadCombat();
        }
        else
        {
            if (upgradeInfoText != null)
                upgradeInfoText.text = "Not enough cultists! Earn more in combat.";
        }
    }
}
