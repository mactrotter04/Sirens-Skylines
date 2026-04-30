using StarterAssets;
using UnityEngine;

public class WallRun : MonoBehaviour
{
    [Header("Wall Run Settings")]
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float wallCheckDistance = 0.7f;
    [SerializeField] float maxAngleFromParallel = 20f;
    [SerializeField] float wallVerticalityTolerance = 10f;
    [SerializeField] float minHeightFromGround = 1f;

    [Header("Wall Run Movement")]
    [SerializeField] float minSpeedForWallRun = 5f;
    [SerializeField] float wallRunSpeed = 8f;
    [SerializeField] float wallRunDuration = 5f;
    [SerializeField] float wallRunGravity = -2f;
    [SerializeField] float wallStickForce = 5f;

    [Header("Wall Jump Settings")]
    [SerializeField] float maxNormalChangeAngle = 50f;
    [SerializeField] float verifyDistanceMargin = 0.2f;
    [SerializeField] float wallNormalSmoothing = 0.25f;

    [Header("Wall Jump Movement")]
    [SerializeField] float wallJumpUpForce = 5f;
    [SerializeField] float wallJumpSideForce = 5f;
    [SerializeField] float wallJumpForwardForce = 4f;
    [SerializeField] float wallJumpAitTime = 0.5f;
    [SerializeField] float wallJumpGravity = -5f;
    [SerializeField] float wallJumpCooldown = 0.3f;

    [Header("Energy Settings")]
    [SerializeField] float wallRunStaminaDrain = 10f;
    [SerializeField] float wallJumpStaminaCost = 5f;

    [Header("Animation Clips")]
    [SerializeField] AnimationClip wallRunLeftClip;
    [SerializeField] AnimationClip wallRunRightClip;
    [SerializeField] AnimationClip wallJumpClip;


    float lastWallJumpTime;
    float wallJumpTimer;
    float wallRunTimer;

    Vector3 wallJumpVelocity;
    Vector3 wallNormal;
    Vector3 wallForward;
    Collider lastWallCollider;

    bool lastJumpHeld;
    bool isWallJumping;
    bool isWallRunning;
    bool isWallLeft;
    bool isWallRight;
    
    CharacterController characterController;
    ThirdPersonController thirdPersonController;
    StarterAssetsInputs starterAssetsInputs;
    ParkourHandler parkourHandler;
    Animator animator;
    Energy energy;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        parkourHandler = GetComponent<ParkourHandler>();
        animator = GetComponent<Animator>();
        energy = GetComponent<Energy>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        bool jumpPressed = starterAssetsInputs.jump && !lastJumpHeld;
        lastJumpHeld = starterAssetsInputs.jump;

        if (thirdPersonController.Grounded && !isWallRunning && !isWallJumping)
        {
            lastWallCollider = null;
        }

        if (isWallJumping)
        {
            TickWallJump();
        }
        else if (isWallRunning)
        {
            TickWallRun(jumpPressed);
        }
        else
        {
            TryStartWallRun();
        }
    }

    bool TrySideProbe(Vector3 sideDirection, out RaycastHit hit)
    {
        Vector3 feet = transform.position;
        Vector3 originWaist = feet + Vector3.up * parkourHandler.waistProbeHeight;
        Vector3 originHead = feet + Vector3.up * parkourHandler.headProbeHeight;

        bool gotWaist = Physics.SphereCast(originWaist, parkourHandler.probeSpherRadius, sideDirection, out RaycastHit waistHit, wallCheckDistance, wallLayer);
        bool gotHead = Physics.SphereCast(originHead, parkourHandler.probeSpherRadius, sideDirection, out RaycastHit headHit, wallCheckDistance, wallLayer);

        if (!gotWaist && !gotHead)
        {
            hit = default;
            return false;
        }

        if (gotWaist)
        {
            hit = waistHit;
        }
        else
        {
            hit = headHit;
        }


        if (lastWallCollider != null && hit.collider == lastWallCollider)
        {
            return false;
        }

        return true;
    }

    bool IsValidWall(Vector3 hitNormal)
    {
        float verticalAngle = Vector3.Angle(hitNormal, Vector3.up);
        if (Mathf.Abs(90f - verticalAngle) > wallVerticalityTolerance) return false;

        Vector3 forwardFlat = transform.forward;
        forwardFlat.y = 0;

        if (forwardFlat.sqrMagnitude < 0.0001f) return false;
        forwardFlat.Normalize();

        Vector3 normalFlat = hitNormal;
        normalFlat.y = 0;
        if (normalFlat.sqrMagnitude < 0.0001f) return false;
        normalFlat.Normalize();

        float deviation = Mathf.Abs(90f - Vector3.Angle(forwardFlat, normalFlat));
        if (deviation > maxAngleFromParallel) return false;

        return true;
    }

    void TryStartWallRun()
    {
        if (thirdPersonController.Grounded) return;
        if (!starterAssetsInputs.sprint) return;
        if (Time.time - lastWallJumpTime < wallJumpCooldown) return;
        if (energy.CurrentEnerg() <= 0) return;

        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        if (horizontalVelocity.magnitude < minSpeedForWallRun) return;

        if (Physics.Raycast(transform.position, Vector3.down, minHeightFromGround, wallLayer)) return;

        bool hitLeft = TrySideProbe(-transform.right, out RaycastHit leftHit);
        bool hitRight = TrySideProbe(transform.right, out RaycastHit rightHit);
        bool leftValid = hitLeft && IsValidWall(leftHit.normal);
        bool rightValid = hitRight && IsValidWall(rightHit.normal);

        if (!leftValid && !rightValid) return;

        if (leftValid && (!rightValid || leftHit.distance <= rightHit.distance))
        {
            isWallLeft = true;
            isWallRight = false;
            wallNormal = leftHit.normal;
        }
        else
        {
            isWallLeft = false;
            isWallRight = true;
            wallNormal = rightHit.normal;
        }

        //Debug.Log(isWallLeft ? "Wall on Left side" : "Wall on Right side");
        isWallRunning = true;
        StartWallRun();
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;

        wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, transform.forward) < 0f)
        {
            wallForward = -wallForward;
        }

        thirdPersonController.enabled = false;
        starterAssetsInputs.jump = false;

        animator.SetBool("WallRunning", true);
        animator.SetBool("FreeFall", false);

        AnimationClip clip = null;
        if (isWallLeft)
        {
            clip = wallRunLeftClip;

        }
        else if (isWallRight)
        {
            clip = wallRunRightClip;
        }

        if (clip != null)
        {
            animator.SetTrigger(clip.name);
        }
    }

    void TickWallRun(bool jumpPressed)
    {
        wallRunTimer += Time.deltaTime;

        animator.SetBool("FreeFall", false);

        energy.CalculateEnergy(-wallRunStaminaDrain * Time.deltaTime);
        if (energy.CurrentEnerg() <= 0f)
        {
            StopWallRun();
            return;
        }

        Vector3 towardWall = -wallNormal;
        Vector3 feet = transform.position;
        Vector3 originWaist = feet + Vector3.up * parkourHandler.waistProbeHeight;
        Vector3 originHead = feet + Vector3.up * parkourHandler.headProbeHeight;
        float verifyDistance = wallCheckDistance + verifyDistanceMargin;

        RaycastHit verifyHit = default;
        bool anyHit = false;
        if (Physics.SphereCast(originWaist, parkourHandler.probeSpherRadius, towardWall, out verifyHit, verifyDistance, wallLayer))
        {
            anyHit = true;
        }
        else if (Physics.SphereCast(originHead, parkourHandler.probeSpherRadius, towardWall, out verifyHit, verifyDistance, wallLayer))
        {
            anyHit = true;
        }

        if (!anyHit)
        {
            StopWallRun();
            return;
        }

        if (Vector3.Angle(wallNormal, verifyHit.normal) > maxNormalChangeAngle)
        {
            StopWallRun();
            return;
        }

        wallNormal = Vector3.Slerp(wallNormal, verifyHit.normal, wallNormalSmoothing).normalized;
        wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, transform.forward) < 0f)
        {
            wallForward = -wallForward;
        }

        if (wallRunTimer >= wallRunDuration || thirdPersonController.Grounded || !starterAssetsInputs.sprint)
        {
            StopWallRun();
            return;
        }

        if (jumpPressed)
        {
            WallJump();
            return;
        }

        Vector3 velocity = wallForward * wallRunSpeed;
        velocity.y = wallRunGravity;
        velocity -= wallNormal * wallStickForce;
        characterController.Move(velocity * Time.deltaTime);
    }

    void StopWallRun()
    {
        if (!isWallRunning) return;
        isWallRunning = false;
        isWallLeft = false;
        isWallRight = false;
        animator.SetBool("WallRunning", false);
        thirdPersonController.enabled = true;
        thirdPersonController._verticalVelocity = wallRunGravity;
    }


    void WallJump()
    {
        wallJumpVelocity = wallNormal * wallJumpSideForce +
                           Vector3.up * wallJumpUpForce +
                           wallForward * wallJumpForwardForce;

        energy.CalculateEnergy(-wallJumpStaminaCost);

        lastWallJumpTime = Time.time;

        isWallRunning = false;
        isWallLeft = false;
        isWallRight = false;
        isWallJumping = true;
        wallJumpTimer = 0f;
        starterAssetsInputs.jump = false;
        animator.SetBool("WallRunning", false);

        if (wallJumpClip != null)
        {
            animator.SetTrigger(wallJumpClip.name);
        }
    }

    void TickWallJump()
    {
        wallJumpTimer += Time.deltaTime;

        wallJumpVelocity.y += wallJumpGravity * Time.deltaTime;

        characterController.Move(wallJumpVelocity * Time.deltaTime);

        if (wallJumpTimer >= wallJumpAitTime || characterController.isGrounded)
        {
            isWallJumping = false;
            thirdPersonController.enabled = true;
            thirdPersonController._verticalVelocity = wallJumpVelocity.y;
        }
    }


    void OnDrawGizmosSelected()
    {
        parkourHandler = GetComponent<ParkourHandler>();

        Vector3 feet = transform.position;
        Vector3 right = transform.right;

        float[] probeHeights = { parkourHandler.waistProbeHeight, parkourHandler.headProbeHeight };
        Vector3[] sideDirections = { -right, right };

        foreach (float height in probeHeights)
        {
            Vector3 origin = feet + Vector3.up * height;

            foreach (Vector3 sideDirection in sideDirections)
            {
                bool hit = Physics.SphereCast(origin, parkourHandler.probeSpherRadius, sideDirection, out _, wallCheckDistance, wallLayer);
                Gizmos.color = hit ? Color.red : Color.green;
                Gizmos.DrawLine(origin, origin + sideDirection * wallCheckDistance);
                Gizmos.DrawWireSphere(origin + sideDirection * wallCheckDistance, parkourHandler.probeSpherRadius);
            }
        }
    }
}
