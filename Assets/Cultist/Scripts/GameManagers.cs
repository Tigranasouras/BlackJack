using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagers : MonoBehaviour
{
    public static GameManagers Instance;

    public int currentLevel = 1;
    public int maxLevel = 3;
    public bool inCombat = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called from UI (e.g., Start Button)
    public void StartGame()
    {
        currentLevel = 1;
        CultManager.Instance.ResetCultists();
        LoadCombat();
    }

    // Load the combat scene.
    public void LoadCombat()
    {
        inCombat = true;
        SceneManager.LoadScene("CombatScene");
    }

    // Load the sacrifice/upgrade scene.
    public void LoadSacrifice()
    {
        inCombat = false;
        SceneManager.LoadScene("SacrificeScene");
    }

    // Called when an enemy is defeated.
    public void NextLevel()
    {
        currentLevel++;
        if (currentLevel > maxLevel)
            LoadVictory();
        else
            LoadSacrifice();
    }

    // Load a victory/win scene.
    public void LoadVictory()
    {
        SceneManager.LoadScene("VictoryScene");
    }
}

//Scene Setup:
//In CombatScene, include an Enemy game object (with the Enemy script attached),
//a UI Slider (assigned to the Enemy’s healthBar),
//and a ClickHandler object (ensure its enemy field points to the enemy instance).

//In SacrificeScene, add UI elements (e.g., text for cultist count, a button for upgrades)
//and attach the UpgradeSystem and UIManager scripts.

//Make sure your VictoryScene displays a message indicating game completion.

//Customization:
//Feel free to adjust parameters (damage values, costs, rewards) according to your game design.
//Add sound effects, simple animations (e.g., scaling when clicking), and visual feedback as desired.