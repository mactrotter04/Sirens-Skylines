using StarterAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.ShaderGraph.Internal;

public class Energy : MonoBehaviour
{
    [Header("Stamina Slider")]
    [SerializeField] Slider energySlider;

    [Header("Stamina")]
    [SerializeField] float energyMax = 100f;
    [SerializeField] float energyLoss = 5f;
    [SerializeField] float energyRegain = 1f;
    [SerializeField, Range(0, 1)] float unlocksprint = 0.25f;

    [SerializeField] float blinkSpeed = 5f;
    [SerializeField] float blinkMinAlpha = 0.25f;

    public float CurrentEnerg() => currentEnergy;
    float currentEnergy;
    float tempSpeed;
    bool canSprint = true;
    public float EnergyMax() => energyMax;

    StarterAssetsInputs inputs;
    ThirdPersonController tpc;
    Image fillImage; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tpc = FindFirstObjectByType<ThirdPersonController>();
        currentEnergy = energyMax;
        tempSpeed = tpc.SprintSpeed;

        energySlider.value = 1f;
        energySlider.minValue = 0f;
        energySlider.maxValue = 1f;

        fillImage = energySlider.fillRect.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        StaminaLoss();
        UpdateEnergy();
    }

    void StaminaLoss()
    {
        float drainPerSecond = energyMax / energyLoss;
        float regainPerSecond = energyMax / energyRegain;

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (sprintHeld && currentEnergy > 0 && canSprint)
        {
            currentEnergy -= drainPerSecond * Time.deltaTime;
        }
        else if(!canSprint || !sprintHeld)
        {
            currentEnergy += regainPerSecond * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, energyMax);

        if(canSprint && currentEnergy <= 0)
        {
            canSprint = false;
            tpc.SprintSpeed = tpc.MoveSpeed;
        }
        else if (!canSprint && currentEnergy >= energyMax * unlocksprint)
        {
            canSprint = true;
            tpc.SprintSpeed = tempSpeed;
        }
    }

    void UpdateEnergy()
    {
        energySlider.value = currentEnergy / energyMax;

        float alpha = 1f;

        if (!canSprint)
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);

            alpha = Mathf.Lerp(blinkMinAlpha, 1f, pulse);
        }

        Color color = fillImage.color;
        color.a = alpha;
        fillImage.color = color;
    }

    public void CalculateEnergy(float energyAmount)
    {
        currentEnergy += energyAmount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, energyMax);
    }

    // float staminaForTrick = 10f
    // Energy engergy
    // start 
    // energy = getcomponent<energy>();
    // energy.CalcualteEnergy(staminaForTrick);w
}
