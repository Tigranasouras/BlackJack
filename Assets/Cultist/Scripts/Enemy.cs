using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthBar;  // Optional: assign a UI Slider to show enemy health.
    public int cultistsReward = 5;  // Cultists earned when enemy is defeated.
    public int damagePerClick = 10;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Called when the enemy is clicked.
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    private void Die()
    {
        // Reward cultists for defeating enemy.
        CultManager.Instance.AddCultists(cultistsReward);
        // Notify GameManager to move to the next phase.
        GameManagers.Instance.NextLevel();
    }
}
