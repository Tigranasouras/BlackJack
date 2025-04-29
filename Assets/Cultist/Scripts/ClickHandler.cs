using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    public Enemy enemy;  // Link this in the Inspector to the enemy in scene.
    public int clickDamage = 10;

    void Update()
    {
        // Mouse click or Space key for attack.
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (enemy != null)
            {
                enemy.TakeDamage(clickDamage);
                // (Optional) Add visual or sound effects here.
            }
        }
    }
}
