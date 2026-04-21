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
    [SerializeField] float EnergyMax = 100f;
    [SerializeField] float energyLoss = 5f;
    [SerializeField] float energyRegain = 1f;
    [SerializeField, Range(0, 1)] float unlocksprint = 0.25f;


    float CurrentEnergy;
    float tempSpeed;
    bool canSprint = true;

    StarterAssetsInputs inputs;
    ThirdPersonController tpc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tpc = FindFirstObjectByType<ThirdPersonController>();
        CurrentEnergy = EnergyMax;
        tempSpeed = tpc.SprintSpeed;

        energySlider.value = 1f;
        energySlider.minValue = 0f;
        energySlider.maxValue = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        StaminaLoss();
        UpdateEnergy();
    }

    void StaminaLoss()
    {
        float drainPerSecond = EnergyMax / energyLoss;
        float regainPerSecond = EnergyMax / energyRegain;

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (sprintHeld && CurrentEnergy > 0 && canSprint)
        {
            CurrentEnergy -= drainPerSecond * Time.deltaTime;
        }
        else if(!sprintHeld)
        {
            CurrentEnergy += regainPerSecond * Time.deltaTime;
        }

        CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0, EnergyMax);

        if(canSprint && CurrentEnergy <= 0)
        {
            canSprint = false;
            tpc.SprintSpeed = tpc.MoveSpeed;
        }
        else if (!canSprint && CurrentEnergy >= EnergyMax * unlocksprint)
        {
            canSprint = true;
            tpc.SprintSpeed = tempSpeed;
        }
    }

    void UpdateEnergy()
    {
        energySlider.value = CurrentEnergy/ EnergyMax;
    }
}
