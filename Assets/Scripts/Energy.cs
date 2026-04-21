using StarterAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Energy : MonoBehaviour
{
    [Header("Stamina Slider")]
    [SerializeField] Slider energySlider;

    [Header("Stamina")]
    [SerializeField] float EnergyMax = 100f;
    [SerializeField] float energyincrements = 0.1f;
    [SerializeField, Range(0, 1)] float unlocksprint = 0.25f;

    float CurrentEnergy;
    float tempSpeed;
    float lastSprintTime;
    bool canSprint = true;

    ThirdPersonController tpc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tpc = FindFirstObjectByType<ThirdPersonController>();
        CurrentEnergy = EnergyMax;
        energySlider.value = CurrentEnergy;
        tempSpeed = tpc.SprintSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        StaminaLoss();
        UpdateEnergy();
        
    }

    void StaminaLoss()
    {
        if (Input.GetKey(KeyCode.LeftShift) && CurrentEnergy > 0 && canSprint)
        {
            CurrentEnergy -= energyincrements * Time.deltaTime;
        }
        else
        {
            CurrentEnergy += energyincrements * Time.deltaTime;
        }
        if(CurrentEnergy  <= 0)
        {
            canSprint = false;
        }
        else if (CurrentEnergy >= EnergyMax * unlocksprint)
        {
            canSprint = true;
        }
    }

    void UpdateEnergy()
    {
        energySlider.value = CurrentEnergy;
    }
}
