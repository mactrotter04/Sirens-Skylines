using StarterAssets;
using System;
using System.Collections;
using System.Net.NetworkInformation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

public class ParkourHandler : MonoBehaviour
{
    [Header("Contact")]
    [SerializeField] float contactDistance;
    [SerializeField] float queueTimeOut = 1f;

    [Header("Probes")]
    [SerializeField] float lowProbeHeight = 0.3f;
    public float waistProbeHeight = 1f;
    public float headProbeHeight = 1.7f;
    [SerializeField] float overheadProbeHight = 2.6f;
    [SerializeField] float impossibleHight = 3.2f;
    public float probeSpherRadius = 0.15f;

    [Header("Climbing")]
    [SerializeField] LayerMask climbableLayer;
    [SerializeField] LayerMask ObstrutionLayer;
    [SerializeField] float ledgeInset = 0.3f;

    [Header("Climbing Cooldowns")]
    [SerializeField] float recoverDuration = 0.25f;
    [SerializeField] float cooldownDuration = 1f;

    [Header("Trigger Peramiters")]
    [SerializeField] float minTriggerSpeed = 6f;
    [SerializeField] float triggerDistance = 1.2f;
    [SerializeField] float ledgeMaxSlope = 35f;

    [SerializeField] float headroomHeight = 1.8f;
    [SerializeField] float headroomRadius = 0.35f;

    [SerializeField] AnimationClip mantleClip;
    [SerializeField] AnimationClip mediumClimbClip;
    [SerializeField] AnimationClip highClimbClip;

    StarterAssetsInputs inputs;
    Animator animator;
    ThirdPersonController tpc;
    BoxCollider boxCollider;
    CharacterController characterController;
    State state = State.Idleing;
    ParkourKind activeKind = ParkourKind.None;
    DetectionResult queuedHit;
    Vector3 startPos, landingPos, risePoint;
    Quaternion startRot, faceWallRot;
    Transform cinemachineTarget;
    Quaternion camtargetStartRot, camTargetEndRot;


    float stateTimer;
    bool lastJumpHeld;
    bool jumpPressed;
    float activeDuration;
    float camTargetEndYaw;




    struct DetectionResult
    {
        public ParkourKind kind;
        public Vector3 ledgeTop;
        public Vector3 wallNormalXZ;
    }

    enum ParkourKind
    {
        None,
        Mantle,
        MediumClimb,
        HighClimb
    }
    enum State
    {
        Idleing,
        Queued,// none mantle midclimb high
        Exacuting,
        Recovery,
        Cooldown
    }

    bool Probe(Vector3 feet, Vector3 fwd, float height, out RaycastHit hit)
    {
        Vector3 origin = feet + Vector3.up * height;
        return Physics.SphereCast(origin, probeSpherRadius, fwd, out hit, triggerDistance, climbableLayer);
    }

    void Awake()
    {
        inputs = GetComponent<StarterAssetsInputs>();
        tpc = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        cinemachineTarget = tpc.CinemachineCameraTarget.transform;
    }


    void Update()
    {
        jumpPressed = inputs.jump && !lastJumpHeld;

        switch (state)
        {
            case State.Idleing:
                TryTrigger();
                break;
            case State.Queued:
                TickQueued();
                break;
            case State.Exacuting:
                TickExecute();
                break;
            case State.Recovery:
                stateTimer += Time.deltaTime;
                if (stateTimer >= recoverDuration)
                {
                    stateTimer = 0f;
                    state = State.Cooldown;
                    tpc.enabled = true;
                    inputs.jump = false;
                    activeKind = ParkourKind.None;
                }
                break;
            case State.Cooldown:
                stateTimer += Time.deltaTime;
                if (stateTimer >= cooldownDuration)
                {
                    stateTimer = 0f;
                    state = State.Idleing;
                }
                break;
        }

        lastJumpHeld = inputs.jump;
    }

    void TryTrigger()
    {
        if (!tpc.Grounded) return;
        if (!jumpPressed) return;

        Vector3 vel = characterController.velocity;
        vel.y = 0f;
        if (vel.magnitude < minTriggerSpeed) return;

        if (!TryDetect(out DetectionResult hit)) return;

        inputs.jump = false;
        queuedHit = hit;
        stateTimer = 0f;
        state = State.Queued;
    }

    bool TryDetect(out DetectionResult result)
    {
        result = default;

        Vector3 feet = transform.position;
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f)
        {
            return false;
        }
        fwd.Normalize();

        bool waist = Probe(feet, fwd, waistProbeHeight, out RaycastHit waistHit);
        bool head = Probe(feet, fwd, headProbeHeight, out RaycastHit headHit);
        bool overhead = Probe(feet, fwd, overheadProbeHight, out RaycastHit overheadHit);
        bool impossible = Probe(feet, fwd, impossibleHight, out RaycastHit impossibleHit);

        if (impossible) return false;
        if (!waist) return false;

        Vector3 wallNoraml = waistHit.normal;
        wallNoraml.y = 0f;

        if (wallNoraml.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        wallNoraml.Normalize();

        Vector3 ledgeProbeOrigin = waistHit.point + fwd * ledgeInset + Vector3.up * (impossibleHight + 0.5f);

        if (!Physics.Raycast(ledgeProbeOrigin, Vector3.down, out RaycastHit ledgeHit, 4f, climbableLayer))
        {
            return false;
        }

        if (Vector3.Angle(ledgeHit.normal, Vector3.up) > ledgeMaxSlope)
        {
            return false;
        }

        ParkourKind kind;

        if (overhead)
        {
            kind = ParkourKind.HighClimb;
        }
        else if (head)
        {
            kind = ParkourKind.MediumClimb;
        }
        else
        {
            kind = ParkourKind.Mantle;
        }

        if (!HasHeadroom(ledgeHit.point, wallNoraml))
        {
            return false;
        }
        if (!HasVerticleClerance(feet, ledgeHit.point.y))
        {
            return false;
        }

        result.kind = kind;
        result.ledgeTop = ledgeHit.point;
        result.wallNormalXZ = wallNoraml;
        return true;
    }

    void TickQueued()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= queueTimeOut)
        {
            state = State.Idleing;
            stateTimer = 0f;
            return;
        }

        if (TryDetect(out DetectionResult fresh))
        {
            queuedHit = fresh;
            EnterExecute(fresh);
        }
    }

    void TickExecute()
    {
        stateTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(stateTimer / activeDuration);

        Vector3 pos;

        if (activeKind == ParkourKind.Mantle)
        {
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            pos = Vector3.Lerp(startPos, landingPos, easedProgress);
            pos.y += MathF.Sin(progress * MathF.PI) * 0.15f;
        }
        else
        {
            const float phase1 = 0.5f;

            if (progress < phase1)
            {
                pos = Vector3.Lerp(startPos, risePoint, progress / phase1);
            }
            else
            {
                pos = Vector3.Lerp(risePoint, landingPos, (progress - phase1) / (1f - phase1));
            }
        }

        transform.position = pos;
        transform.rotation = Quaternion.Slerp(startRot, faceWallRot, progress);

        cinemachineTarget.rotation = Quaternion.Slerp(camtargetStartRot, camTargetEndRot, progress);

        if (progress >= 1f)
        {
            transform.position = landingPos;
            transform.rotation = faceWallRot;
            cinemachineTarget.rotation = camTargetEndRot;
            tpc._cinemachineTargetYaw = camTargetEndYaw;
            ResolvePenetration();
            characterController.enabled = true;
            inputs.jump = false;
            inputs.move = Vector2.zero;
            stateTimer = 0f;
            state = State.Recovery;
        }
    }

    void ResolvePenetration()
    {
        var cc = characterController;
        Collider[] near = Physics.OverlapCapsule(transform.position + Vector3.up * cc.radius, transform.position + Vector3.up * (cc.height - cc.radius), cc.radius, ObstrutionLayer);
        
        for (int pass = 0; pass < 3 && near.Length > 0; pass++)
        {
            bool pushed = false;
            foreach(var col in near)
            {
                if(Physics.ComputePenetration(cc, transform.position, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 dir, out float dist))
                {
                    transform.position += dir * (dist + 0.01f);
                    pushed = true;
                }
            }
            if (!pushed) break;
            near = Physics.OverlapCapsule(transform.position + Vector3.up * cc.radius, transform.position + Vector3.up * (cc.height - cc.radius),cc.radius, ObstrutionLayer);
        }
    }

    bool HasHeadroom(Vector3 ledgePoint, Vector3 wallNormalXZ)
    {
        const float landingLift = 0.05f;
        Vector3 standPos = ledgePoint + (-wallNormalXZ) * ledgeInset + Vector3.up * landingLift;
        Vector3 p1 = standPos + Vector3.up * headroomRadius;
        Vector3 p2 = standPos + Vector3.up * (headroomHeight - headroomRadius);
        return !Physics.CheckCapsule(p1, p2, headroomRadius, ObstrutionLayer);
    }

    bool HasVerticleClerance(Vector3 feet, float ledgeTopY)
    {
        float r = characterController.radius * 0.5f;
        float rise = ledgeTopY - feet.y;
        if (rise <= r * 2f) return true;
        Vector3 p1 = feet + Vector3.up * r;
        Vector3 p2 = new Vector3(feet.x, ledgeTopY - r, feet.z);

        return !Physics.CheckCapsule(p1, p2, r, ObstrutionLayer);
    }

    void EnterExecute(DetectionResult hit)
    {
        activeKind = hit.kind;

        AnimationClip clip = hit.kind switch
        {
            ParkourKind.Mantle => mantleClip,
            ParkourKind.MediumClimb => mediumClimbClip,
            ParkourKind.HighClimb => highClimbClip,
            _ => null
        };

        activeDuration = clip != null ? clip.length : 1f;

        startPos = transform.position;
        startRot = transform.rotation;

        landingPos = hit.ledgeTop + (-hit.wallNormalXZ) * ledgeInset + Vector3.up * 0.02f;

        risePoint = new Vector3(startPos.x, hit.ledgeTop.y + 0.15f, startPos.z);

        faceWallRot = quaternion.LookRotation(-hit.wallNormalXZ, Vector3.up);

        camtargetStartRot = cinemachineTarget.rotation;
        camTargetEndYaw = faceWallRot.eulerAngles.y;
        camTargetEndRot = Quaternion.Euler(camtargetStartRot.eulerAngles.x, camTargetEndYaw, 0f);

        tpc.enabled = false;
        characterController.enabled = false;

        if(animator != null && clip != null)
        {
            animator.SetTrigger(clip.name);
        }
        
        stateTimer = 0f;
        state = State.Exacuting;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 feet = transform.position;
        Vector3 fwd = transform.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < Mathf.Epsilon) return;

        fwd.Normalize();

        float[] probeHeights = { lowProbeHeight, waistProbeHeight, headProbeHeight, overheadProbeHight, impossibleHight };

        foreach (float height in probeHeights)
        {
            Vector3 origin = transform.position + Vector3.up * height;

            bool hit = Physics.SphereCast(origin, probeSpherRadius, fwd, out _, triggerDistance, climbableLayer);

            Gizmos.color = hit ? Color.red : Color.green;
            Gizmos.DrawLine(origin, origin + fwd * triggerDistance);
            Gizmos.DrawWireSphere(origin + fwd * contactDistance, probeSpherRadius);
        }


    }
}
