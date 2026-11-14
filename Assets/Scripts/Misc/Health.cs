using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    public int MaxHealth => maxHealth;
    public int currentHealth { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth == 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"{name} 受到 {damage} 点伤害，剩余 {currentHealth}/{maxHealth}", this);
    }
}
