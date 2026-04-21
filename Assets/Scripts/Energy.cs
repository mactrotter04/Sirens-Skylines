using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class Energy : MonoBehaviour
{
    [SerializeField] Slider energySlider;
    [SerializeField] float EnergyMax = 100f;
    [SerializeField] float EnergyLoss = 1f;
    float CurrentEnergy;
    float tempSpeed;

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
        if(Input.GetKey(KeyCode.LeftShift) && CurrentEnergy > 0)
        {
            CurrentEnergy -= EnergyLoss * Time.deltaTime;
            CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0f , EnergyMax);
        }
    }

    void UpdateEnergy()
    {
        energySlider.value = CurrentEnergy;
    }
}
