using System.Collections;
using UnityEngine;

public class FInalDestiantionPointer : MonoBehaviour
{
    [SerializeField] Transform destination;
    [SerializeField] float secondsBeforeAppear = 5f;
    MeshRenderer meshRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.enabled = false;
        StartCoroutine(AppearAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(destination);
    }

    IEnumerator AppearAfterDelay()
    {
        yield return new WaitForSeconds(secondsBeforeAppear);
        meshRenderer.enabled = true;
    }
}
