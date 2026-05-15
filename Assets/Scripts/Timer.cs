using System;
using System.Collections;
using System.Linq.Expressions;
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
    [SerializeField] float watchCheckDuration = 5f;


    PlayerHealth playerHealth;

    float currentTime;
    bool bleed = false;
    bool checkingTime = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timerText.gameObject.SetActive(false);
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        currentTime = initialTime;
        timerText.text = TimeSpan.FromSeconds(currentTime).ToString(@"mm\:ss");
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(CheckTime());

        if (currentTime > 0)
        {
            float previos = currentTime;

            currentTime -= Time.deltaTime;

            currentTime = Mathf.Max(currentTime, 0f);

            if (previos > 30f && currentTime <= 30f)
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
        while (true)
        {
            timerText.color = Color.red;
            yield return new WaitForSeconds(healthFlash);
            timerText.color = Color.blue;
            yield return new WaitForSeconds(healthFlash);
        }
    }

    IEnumerator CheckTime()
    {
        if (Input.GetKeyDown(KeyCode.T) && !checkingTime)
        {
            checkingTime = true;
            timerText.gameObject.SetActive(true);
            yield return new WaitForSeconds(watchCheckDuration);
            timerText.gameObject.SetActive(false);
            checkingTime = false;
        }
    }
}
