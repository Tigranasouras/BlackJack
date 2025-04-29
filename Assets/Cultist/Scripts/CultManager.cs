using UnityEngine;
using UnityEngine.UI;

public class CultManager : MonoBehaviour
{
    public static CultManager Instance;

    public int cultistCount = 0;
    public Text cultistUIText;  // Assign a UI Text element in the Inspector.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally, persist across scenes if desired.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Call this when the player earns cultists
    public void AddCultists(int amount)
    {
        cultistCount += amount;
        UpdateCultistUI();
    }

    // Call this to spend cultists for upgrades.
    public bool SpendCultists(int amount)
    {
        if (cultistCount >= amount)
        {
            cultistCount -= amount;
            UpdateCultistUI();
            return true;
        }
        return false;
    }

    public void ResetCultists()
    {
        cultistCount = 0;
        UpdateCultistUI();
    }

    private void UpdateCultistUI()
    {
        if (cultistUIText != null)
            cultistUIText.text = "Cultists: " + cultistCount;
    }
}
