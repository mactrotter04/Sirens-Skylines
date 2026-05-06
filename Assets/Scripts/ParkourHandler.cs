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
    [SerializeField] float landingInset = 0.3f;

    [Header("Climbing Cooldowns")]
    [SerializeField] float recoverDuration = 0.25f;
    [SerializeField] float cooldownDuration = 1f;

    [Header("Trigger Peramiters")]
    [SerializeField] float minTriggerSpeed = 6f;
    [SerializeField] float triggerDistance = 1.2f;
    [SerializeField] float ledgeMaxSlope = 35f;

    [Header("Climb Sizes")]
    [SerializeField] float headroomHeight = 1.8f;
    [SerializeField] float headroomRadius = 0.35f;

    [Header("Animations")]
    [SerializeField] AnimationClip mantleClip;
    [SerializeField] AnimationClip mediumClimbClip;
    [SerializeField] AnimationClip highClimbClip;

    [Header("Ledge Search")]
    [SerializeField] float ledgeStepSize = 0.04f;
    [SerializeField] float ledgeMaxSerch = 0.6f;
    [SerializeField] float ledgeRayHeightAbove = 0.5f;
    [SerializeField] float ledgeRayDistance = 4f;

    [Header("Climb Motion")]
    [SerializeField] float mantleArkHeight = 0.15f;
    [SerializeField] [Range (0f, 1f)] float climbPhaseSplit = 0.5f;
    [SerializeField] float risePointLift = 0.15f;
    [SerializeField] float landingFinalLift = 0.02f;
    [SerializeField] float standPosLift = 0.05f;
    [SerializeField] float fallbackAnimDuration = 1f;

    [Header("Penitration Resolve")]
    [SerializeField] int penetrationResolvePasses = 3;
    [SerializeField] float penetrationSepartaionBuffer = 0.01f;
    [SerializeField] float verticalCleranceRadiusMult = 0.5f;

    StarterAssetsInputs inputs;
    Animator animator;
    ThirdPersonController tpc;
    BoxCollider boxCollider;
    CharacterController characterController;
    Transform cinemachineTarget;

    State state = State.Idleing;
    ParkourKind activeKind = ParkourKind.None;

    DetectionResult queuedHit;
    Vector3 startPos, landingPos, risePoint;
    Quaternion startRot, faceWallRot;
    Quaternion camtargetStartRot, camTargetEndRot;

    bool lastJumpHeld;
    bool jumpPressed;

    float activeDuration;
    float camTargetEndYaw;
    float stateTimer;

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

        RaycastHit ledgeHit = default;
        bool ledgeOk = false;

        for (float inset = ledgeStepSize; inset <= ledgeMaxSerch; inset += ledgeStepSize)
        {
            Vector3 origin = waistHit.point + fwd * inset + Vector3.up * (impossibleHight + ledgeRayHeightAbove);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit candiate, ledgeRayDistance, climbableLayer))
            {
                continue;
            }

            if(Vector3.Angle(candiate.normal, Vector3.up) > ledgeMaxSlope)
            {
                continue;
            }

            ledgeHit = candiate;
            ledgeOk = true;
            break;
        }

        if (!ledgeOk)
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
            pos.y += MathF.Sin(progress * MathF.PI) * mantleArkHeight;
        }
        else
        {
            if (progress < climbPhaseSplit)
            {
                pos = Vector3.Lerp(startPos, risePoint, progress / climbPhaseSplit);
            }
            else
            {
                pos = Vector3.Lerp(risePoint, landingPos, (progress - climbPhaseSplit) / (1f - climbPhaseSplit));
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

        for (int pass = 0; pass < penetrationResolvePasses && near.Length > 0; pass++)
        {
            bool pushed = false;
            foreach (var col in near)
            {
                if (Physics.ComputePenetration(cc, transform.position, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 dir, out float dist))
                {
                    transform.position += dir * (dist + penetrationSepartaionBuffer);
                    pushed = true;
                }
            }
            if (!pushed) break;
            near = Physics.OverlapCapsule(transform.position + Vector3.up * cc.radius, transform.position + Vector3.up * (cc.height - cc.radius), cc.radius, ObstrutionLayer);
        }
    }

    bool HasHeadroom(Vector3 ledgePoint, Vector3 wallNormalXZ)
    {
        Vector3 standPos = ledgePoint + (-wallNormalXZ) * landingInset + Vector3.up * standPosLift;
        Vector3 p1 = standPos + Vector3.up * headroomRadius;
        Vector3 p2 = standPos + Vector3.up * (headroomHeight - headroomRadius);
        return !Physics.CheckCapsule(p1, p2, headroomRadius, ObstrutionLayer);
    }

    bool HasVerticleClerance(Vector3 feet, float ledgeTopY)
    {
        float r = characterController.radius * verticalCleranceRadiusMult;
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

        if(clip != null)
        {
            activeDuration = clip.length;
        }
        else
        {
            activeDuration = fallbackAnimDuration;
        }

        startPos = transform.position;
        startRot = transform.rotation;

        landingPos = hit.ledgeTop + (-hit.wallNormalXZ) * landingInset + Vector3.up * landingFinalLift;

        risePoint = new Vector3(startPos.x, hit.ledgeTop.y + risePointLift, startPos.z);

        faceWallRot = quaternion.LookRotation(-hit.wallNormalXZ, Vector3.up);

        camtargetStartRot = cinemachineTarget.rotation;
        camTargetEndYaw = faceWallRot.eulerAngles.y;
        camTargetEndRot = Quaternion.Euler(camtargetStartRot.eulerAngles.x, camTargetEndYaw, 0f);

        tpc.enabled = false;
        characterController.enabled = false;

        if (animator != null && clip != null)
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
