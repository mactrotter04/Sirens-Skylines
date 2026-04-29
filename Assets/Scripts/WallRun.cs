using StarterAssets;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class WallRun : MonoBehaviour
{
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float wallCheckDistance = 0.7f;

    [SerializeField] float maxAngleFromParallel = 20f;
    [SerializeField] float wallVerticalityTolerance = 10f;

    [SerializeField] float minSpeedForWallRun = 5f;
    [SerializeField] float minHeightFromGround = 1f;

    [SerializeField] float wallRunSpeed = 8f;
    [SerializeField] float wallRunDuration = 5f;
    [SerializeField] float wallRunGravity = -2f;
    [SerializeField] float wallStickForce = 5f;

    bool isWallrunning;
    bool isWallLeft;
    bool isWallRight;
    Vector3 wallNormal;
    float wallRunTimer;
    Vector3 wallForward;

    CharacterController characterController;
    ThirdPersonController thirdPersonController;
    StarterAssetsInputs starterAssetsInputs;
    ParkourHandler parkourHandler;
    Animator animator;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        parkourHandler = GetComponent<ParkourHandler>();
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isWallrunning)
        {
            TickWallRun();
        }
        TryStartWallRun();
    }

    bool TrySideProbe(Vector3 sideDirection, out RaycastHit hit)
    {
        Vector3 feet = transform.position;
        Vector3 originWaist = feet + Vector3.up * parkourHandler.waistProbeHeight;
        Vector3 originHead = feet + Vector3.up * parkourHandler.headProbeHeight;

        bool gotWaist = Physics.SphereCast(originWaist, parkourHandler.probeSpherRadius, sideDirection, out RaycastHit waistHit, wallCheckDistance, wallLayer);
        bool gotHead = Physics.SphereCast(originWaist, parkourHandler.probeSpherRadius, sideDirection, out RaycastHit headHit, wallCheckDistance, wallLayer);

        if (!gotWaist && gotHead)
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

        return true;
    }

    void TryStartWallRun()
    {
        if (thirdPersonController.Grounded) return;
        if (!starterAssetsInputs.sprint) return;

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

        Debug.Log(isWallLeft ? "wall on left side" : "wall on right side");
        isWallrunning = true;
        StartWallRun();
    }

    bool IsValidWall(Vector3 hitNormal)
    {
        float verticleAngle = Vector3.Angle(hitNormal, Vector3.up);
        if (Mathf.Abs(90f - verticleAngle) > wallVerticalityTolerance) return false;

        Vector3 ForwardFlat = transform.forward;
        ForwardFlat.y = 0;

        if (ForwardFlat.sqrMagnitude < 0.0001f) return false;
        ForwardFlat.Normalize();

        Vector3 normalFlat = hitNormal;
        normalFlat.y = 0f;
        if (normalFlat.sqrMagnitude < 0.0001f) return false;
        normalFlat.Normalize();

        float deviation = Mathf.Abs(90f - Vector3.Angle(ForwardFlat, normalFlat));
        if (deviation > maxAngleFromParallel) return false;

        return true;
    }

    void StartWallRun()
    {
        isWallrunning = true;
        wallRunTimer = 0f;

        wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, transform.forward) < 0f)
        {
            wallForward = -wallForward;
        }

        thirdPersonController.enabled = false;
        starterAssetsInputs.jump = false;
    }

    void StopWallRun()
    {
        if(!isWallrunning) return;

        isWallrunning = false;
        isWallLeft = false;
        isWallRight = false;

        thirdPersonController.enabled = true;
        thirdPersonController._verticalVelocity = wallRunGravity;
    }

    void TickWallRun()
    {
        wallRunTimer += Time.deltaTime;

        if (wallRunTimer >= wallRunDuration || thirdPersonController.Grounded || !starterAssetsInputs.sprint)
        {
            StopWallRun();
            return;
        }

        Vector3 velocity = wallForward * wallRunSpeed;
        velocity.y = wallRunGravity;
        velocity -= wallNormal * wallStickForce;
        characterController.Move(velocity * Time.deltaTime);
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
