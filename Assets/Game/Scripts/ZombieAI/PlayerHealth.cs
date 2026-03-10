using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public Image healthFillImage;

    private float maxHealth;

    private void Start()
    {
        maxHealth = health;
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        UpdateHealthUI();
        if (health <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = health / maxHealth;
    }

    private void Die()
    {
        GameManager.Instance.LoseGame();
    }
}