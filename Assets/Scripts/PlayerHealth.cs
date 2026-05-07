using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player health")]
    [SerializeField] float hP = 1000000f;
    [SerializeField] TextMeshProUGUI healthText;

    void Start()
    {
        healthText.text = $"${hP:N0}";
    }

    public void TakeDamage(float damage)
    {
        hP -= damage;
        healthText.text = $"${hP:N0}";

        if (hP <= 0)
        {
            GetComponent<DeathHandler>().HandleDeath();
        }
    }
}
