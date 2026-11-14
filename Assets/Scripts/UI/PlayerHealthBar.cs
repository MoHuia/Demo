using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image fill;
    [SerializeField] private Text value;

    private int m_shownHealth = -1;
    private int m_shownMaxHealth = -1;

    private void LateUpdate()
    {
        if (playerHealth == null || fill == null || value == null)
        {
            return;
        }

        int current = playerHealth.currentHealth;
        int max = playerHealth.MaxHealth;

        if (current == m_shownHealth && max == m_shownMaxHealth)
        {
            return;
        }

        m_shownHealth = current;
        m_shownMaxHealth = max;
        fill.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        value.text = $"{current}/{max}";
    }
}
