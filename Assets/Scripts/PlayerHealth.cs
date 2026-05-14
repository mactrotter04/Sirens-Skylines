using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player health")]
    [SerializeField] float hP = 1000000f;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] TextMeshProUGUI damageText;
    [SerializeField] float damageDisplayDuration = 1f;
    

    void Start()
    {
        damageText.enabled = false;
        healthText.text = $"${hP:N0}";
    }

    public void TakeDamage(float damage)
    {
        hP -= damage;
        healthText.text = $"${hP:N0}";
        StartCoroutine(DisplayDamage(damage));

        if (hP <= 0)
        {
            FindFirstObjectByType<DeathHandler>().HandleDeath();
        }
    }

    IEnumerator DisplayDamage(float damage)
    {
        if (damageText == null) yield break;

        damageText.enabled = true;
        damageText.text = $"-${damage:N0}";

        yield return new WaitForSeconds(damageDisplayDuration);

        damageText.enabled = false;
    }
}
