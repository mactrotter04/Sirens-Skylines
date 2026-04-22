using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

public class ParkourHandler : MonoBehaviour
{
    [SerializeField] float lowProbeHeight = 1f;
    [SerializeField] LayerMask climbableLayer;
    [SerializeField] float contactDistance;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ProcessRaycast();
    }

    void ProcessRaycast()
    {
        Vector3 fwd = transform.forward;
        Vector3 feet = transform.position;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < Mathf.Epsilon)
            return;
        fwd.Normalize();

        Vector3 origin = feet + Vector3.up * lowProbeHeight;
        if (Physics.Raycast(origin, fwd, out RaycastHit hit, contactDistance, climbableLayer))
        {
            Debug.Log("hit a wall");
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 fwd =transform.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < Mathf.Epsilon) return;

        fwd.Normalize();

        Vector3 origin = transform.position + Vector3.up * lowProbeHeight;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + transform.forward * contactDistance);
    }
}
