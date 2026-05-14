using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Timer : MonoBehaviour
{
    [SerializeField] float initialTime = 180f;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Vector2 damageRange = new Vector2(100000f, 200000f);
    [SerializeField] float bleedInterval = 5f;
    [SerializeField] float healthFlash = 2f;


    PlayerHealth playerHealth;

    float currentTime;
    bool bleed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        currentTime = initialTime;
        timerText.text = TimeSpan.FromSeconds(currentTime).ToString(@"mm\:ss");
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 30 && !bleed)
            {
                StartCoroutine(ColorFlash());
            }

            if (currentTime <= 0 && !bleed)
            {
                bleed = true;
                StartCoroutine(BleedOut());
            }
        }

        timerText.text = TimeSpan.FromSeconds(currentTime).ToString(@"mm\:ss");
    }

    

    IEnumerator BleedOut()
    {
        while (true)
        {
            float damage = Random.Range(damageRange.x, damageRange.y);
            playerHealth.TakeDamage(damage);
            yield return new WaitForSeconds(bleedInterval);
        }
    }

    IEnumerator ColorFlash()
    {
        timerText.color = Color.red;
        yield return new WaitForSeconds(healthFlash);
        timerText.color = Color.blue;
        yield return new WaitForSeconds(healthFlash);
    }
}
