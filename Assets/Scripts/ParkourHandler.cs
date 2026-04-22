using StarterAssets;
using System.Collections;
using System.Net.NetworkInformation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

public class ParkourHandler : MonoBehaviour
{
    
    [SerializeField] float contactDistance;
    [SerializeField] float queueTimeOut = 1f;

    [Header("Probe Hights")]
    [SerializeField] float feetProbeHeight = 0.5f;
    [SerializeField] float chestProbeHeight = 1f;
    [SerializeField] float headProbeHeight = 1.6f;
    [SerializeField] float overheadProbeHight = 2.10f;
    [SerializeField] float noClimbHight = 2.90f;

    [Header("Climbing")]
    [SerializeField] LayerMask climbableLayer;
    [SerializeField] float ledgeInset = 0.3f;
    [SerializeField] float mantleDuration = 0.5f;
    [SerializeField] float mediumClimbDuraion = 1f;
    [SerializeField] float highClimbDuration = 2f;

    [Header("Climbing Cooldowns")]
    [SerializeField] float recoverDuration = 0.25f;
    [SerializeField] float cooldownDuration = 1f;

    StarterAssetsInputs inputs;
    Animator animator;
    State state;
    ThirdPersonController tpc;
    BoxCollider boxCollider;
    CharacterController characterController;


    float queuedAt;



    enum ParkourKind
    {
        None,
        Mantle,
        MediumClimb,
        HighClimb
    }

    ParkourKind Queued;

    enum State
    {
        Idleing,
        Queued,// none mantle midclimb high
        Exicuting,
        Recovery,
        Cooldown
    }

    int Hit(Vector3 feet, Vector3 fwd, float height) => Physics.Raycast(feet + Vector3.up * height, fwd, out RaycastHit Hit, contactDistance, climbableLayer) ? 1 : 0; //if physics raycast is true return 1 if not return 0



    ParkourKind Classify(Vector3 feet, Vector3 fwd)
    {
        if (Hit(feet, fwd, feetProbeHeight) == 0) return ParkourKind.None;
        if (Hit(feet, fwd, chestProbeHeight) == 0) return ParkourKind.Mantle;
        if (Hit(feet, fwd, headProbeHeight) == 0) return ParkourKind.MediumClimb;
        if (Hit(feet, fwd, overheadProbeHight) == 0) return ParkourKind.HighClimb;
        return ParkourKind.None;
    }

    float Duration(ParkourKind kind) =>
        kind switch
        {
            ParkourKind.Mantle => mantleDuration,
            ParkourKind.MediumClimb => mediumClimbDuraion,
            ParkourKind.HighClimb => highClimbDuration,
            _ => 0f
        };

    void Awake()
    {
        inputs = GetComponent<StarterAssetsInputs>();
        tpc = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
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

        Vector3 origin = feet + Vector3.up * feetProbeHeight;
        if (Physics.Raycast(origin, fwd, out RaycastHit hit, contactDistance, climbableLayer))
        {
            var kind = Classify(feet, fwd);

            
            boxCollider = hit.collider.GetComponent<BoxCollider>();



            if (state == State.Idleing && inputs.jump && kind != ParkourKind.None)
            {
                state = State.Queued;
                Queued = kind;
                queuedAt = Time.time;
                inputs.jump = false;
                Debug.Log($"Queued {kind} at {queuedAt}");
            }
        }

        if(state == State.Queued && Time.time - queuedAt > queueTimeOut)
        {
            state = State.Idleing;
            Debug.Log($"Queue timed out back to tidle after {Time.time - queuedAt} seconds");
        }

        if (state == State.Queued && Physics.Raycast(origin, fwd, out RaycastHit hit2, contactDistance, climbableLayer))
        {
            Vector3 top = hit2.point + fwd * ledgeInset + Vector3.up * chestProbeHeight;
            StartCoroutine(StartClimb(top, Duration(Queued)));
        }
    }

    IEnumerator StartClimb(Vector3 target, float duration)
    {
        state = State.Exicuting;
        tpc.enabled = false;
        characterController.enabled = false;

        Vector3 startpos = transform.position;
        float time = 0f;

        while(time < duration)
        {
            time += Time.deltaTime;
            transform.position = Vector3.Lerp(startpos, target, time / duration);
            yield return null;
        }

        characterController.enabled = true;
        tpc.enabled = true;
        state = State.Recovery;
        yield return new WaitForSeconds(recoverDuration);
        state = State.Cooldown;
        yield return new WaitForSeconds(cooldownDuration);
        state = State.Idleing;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 fwd =transform.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < Mathf.Epsilon) return;

        fwd.Normalize();

        float[] probeHeights = { feetProbeHeight, chestProbeHeight, headProbeHeight, overheadProbeHight, noClimbHight };
        
        foreach(float height in probeHeights)
        {
            Vector3 origin = transform.position + Vector3.up * height;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + fwd * contactDistance);
        }
    }
}
