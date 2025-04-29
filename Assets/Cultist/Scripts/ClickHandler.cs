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

//public SpriteRenderer enemySprite;  // Drag from enemy's GameObject

//IEnumerator FlashRed()
//{
//    Color original = enemySprite.color;
//    enemySprite.color = Color.red;
//    yield return new WaitForSeconds(0.1f);
//    enemySprite.color = original;
//}

// public AudioSource clickAudio;  // Drag AudioSource here in Inspector.
//if (enemy != null)
//{
//    enemy.TakeDamage(clickDamage);
//
//    if (clickAudio != null)
//        clickAudio.Play();
//
//    if (enemySprite != null)
//        StartCoroutine(FlashRed());
//}