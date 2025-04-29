using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text levelText;        // Display current level.
    public Text instructionText;  // For instructions.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // If you want to persist UI across scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Call this from each scene to update the current level info.
    public void UpdateLevelInfo()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + GameManagers.Instance.currentLevel;
        }
    }

    public void SetInstruction(string msg)
    {
        if (instructionText != null)
        {
            instructionText.text = msg;
        }
    }
}
