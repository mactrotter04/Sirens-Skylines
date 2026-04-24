using UnityEngine;

public class MainMenui : MonoBehaviour
{
    Animator animator;

    bool swap = false;
    float time = 0f;
    float timeBetweenSwitch = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        if (time >= timeBetweenSwitch)
        {
            swap = !swap;
            animator.SetBool("Switch", swap);
            time = 0f;
        }
    }
}
