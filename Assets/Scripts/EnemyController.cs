using System.Collections;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    PlayerHealth target; //player
    [SerializeField] float chaseramge = 10f;
    [SerializeField] float turnSpeed = 1f;


    [Header("idle Zone")]
    [SerializeField] float wanderRadius = 100f;
    [SerializeField] float minIdleTime = 2f;
    [SerializeField] float maxIdleTime = 5f;
    [SerializeField] float sampleingDistance = 1f;

    [Header("Speed")]
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float chaseSpeed = 6.5f;

    [Header("Damage")]
    [SerializeField] Vector2 damageRange = new Vector2(40000f, 120000f);

    [Header("Shooting")]
    [SerializeField] Transform firePoint;
    [SerializeField] Transform aimPoint;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float timeBetweenShots = 2f;
    [SerializeField] float windupTime = 0.5f;
    [SerializeField] float bulletSpeed = 15f;
    [SerializeField] float bulletMaxDistance = 50f;
    [SerializeField] float bulletHitRadius = 0.1f;
    [SerializeField] float bulletTrailLength = 1.5f;

    [Header("Arms")]
    [SerializeField] Transform[] aimArmBones;
    [SerializeField] Vector3 pitchAxisLocal = new Vector3(1, 0, 0);
    [SerializeField] float maxPitchUp = 60f;
    [SerializeField] float maxPitchDown = 60f;
    [SerializeField] float pitchSmoothing = 10f;

    float currentPitch;
    float distanceToTarget = Mathf.Infinity;
    float idleTimer = 0f;
    float nextShotTime;
    bool windupInFlight;
    bool isProvoked = false;

    Animator animator;
    NavMeshAgent navMeshAgent;
    EnemyHealth enemyHelath;

    Vector3 wanderOrigin;


    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        target = FindFirstObjectByType<PlayerHealth>();
        enemyHelath = GetComponent<EnemyHealth>();

        wanderOrigin = transform.position;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    void Update()
    {
        if (enemyHelath.IsDead())
        {
            enabled = false;
            navMeshAgent.enabled = false;
        }

        distanceToTarget = Vector3.Distance(target.transform.position, transform.position);

        if (isProvoked)
        {
            Engagetarget();
        }
        else if (distanceToTarget <= chaseramge)
        {
            isProvoked = true;
        }
        else
        {
            Wander();
        }

        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        animator.SetFloat("MotionSpeed", 1f);
    }

    void LateUpdate()
    {
        if (!isProvoked) return;
        if (aimPoint == null || firePoint == null) return;
        if (aimArmBones == null || aimArmBones.Length == 0) return;
        if (enemyHelath != null && enemyHelath.IsDead()) return;

        Vector3 toAim = aimPoint.position - firePoint.position;
        Vector3 flat = new Vector3(toAim.x, 0f, toAim.z);
        float horizontalDistance = flat.magnitude;

        float desiredPitch = -Mathf.Atan2(toAim.y, horizontalDistance) * Mathf.Rad2Deg;

        desiredPitch = Mathf.Clamp(desiredPitch, -maxPitchDown, maxPitchUp);

        currentPitch = Mathf.Lerp(currentPitch, desiredPitch, pitchSmoothing * Time.deltaTime);

        Quaternion pitchDelta = Quaternion.AngleAxis(currentPitch, pitchAxisLocal);

        for (int i = 0; 1 < aimArmBones.Length; i++)
        {
            Transform bone = aimArmBones[i];
            if(bone == null) continue;
            bone.localRotation *= pitchDelta;
        }
    }

    void Engagetarget()
    {

        if (distanceToTarget >= navMeshAgent.stoppingDistance)
        {
            Chasetarget();
        }
        else
        {
            FaceTarget();
            AttackTarget();
        }
    }

    void Chasetarget()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = chaseSpeed;
        animator.SetBool("Shoot", false);
        navMeshAgent.SetDestination(target.transform.position);
    }

    void AttackTarget()
    {
        //animator.SetBool("Chase", false);

        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;

        if (enemyHelath.IsDead()) return;
        if (windupInFlight) return;
        if (Time.time < nextShotTime) return;

        nextShotTime = Time.time + timeBetweenShots;
        StartCoroutine(Shooting());
    }

    void FaceTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;

        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    void Wander()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            //GetComponent<Animator>().SetBool("chase", false);

            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
            {
                if (GetNewWanderDestination(out Vector3 destination))
                {
                    //GetComponent<Animator>().SetTrigger("Move");
                    navMeshAgent.speed = walkSpeed;
                    navMeshAgent.SetDestination(destination);
                }

                idleTimer = Random.Range(minIdleTime, maxIdleTime);
            }
        }
    }

    bool GetNewWanderDestination(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = wanderOrigin + Random.insideUnitSphere * wanderRadius;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sampleingDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = wanderOrigin;
        return false;
    }

    public void OnDamageTaken()
    {
        isProvoked = true;
    }


    IEnumerator Shooting()
    {
        windupInFlight = true;
        animator.SetBool("Shoot", true);
        yield return new WaitForSeconds(windupTime);

        ////Vector3 dir = aimPoint.position - arms[0].position;
        ////float angle = Mathf.Atan2(-dir.normalized.y) * Mathf.Rad2Deg;
        ////arms[0].localRotation = Quaternion.AngleAxis(angle, Vector3.right);

        if (!enemyHelath.IsDead() && target != null && firePoint != null && lineRenderer != null)
        {
            Vector3 origin = firePoint.position;
            Vector3 aimAt = aimPoint.position;
            Vector3 toTarget = aimAt - origin;
            Vector3 direction = toTarget.normalized;

            float targetDistance =  bulletMaxDistance;

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin);
            lineRenderer.enabled = true;

            float currentDistance = 0f;

            while (currentDistance < targetDistance)
            {
                float lastDistance = currentDistance;
                currentDistance = Mathf.Min(currentDistance + bulletSpeed * Time.deltaTime, targetDistance);
                float stepLength = currentDistance - lastDistance;

                Vector3 castorigin = origin + direction * lastDistance;
                if (Physics.SphereCast(castorigin, bulletHitRadius, direction, out RaycastHit hit, stepLength))
                {
                    float tipDistance = lastDistance + hit.distance;
                    float tailDistance = Mathf.Max(0f, tipDistance - bulletTrailLength);
                    lineRenderer.SetPosition(0, origin + direction * tailDistance);
                    lineRenderer.SetPosition(1, hit.point);

                    PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        float damage = Random.Range(damageRange.x, damageRange.y);
                        playerHealth.TakeDamage(damage);
                    }
                    break;
                }

                float currentTailDistance = Mathf.Max(0f, currentDistance - bulletTrailLength);
                lineRenderer.SetPosition(0, origin + direction * currentTailDistance);
                lineRenderer.SetPosition(1, origin + direction * currentDistance);

                yield return null;
            }

            yield return null;
            lineRenderer.enabled = false;
        }

        windupInFlight = false;
    }




    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseramge);

        Gizmos.color = Color.hotPink;
        Gizmos.DrawWireSphere(wanderOrigin, wanderRadius);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(wanderOrigin, 1f); // current pos

        Gizmos.color = Color.blue;
        if (!Application.isPlaying && navMeshAgent != null)
        {
            Gizmos.DrawSphere(navMeshAgent.destination, 1f); // destination pos 
        }
    }

    //NavMesh.SamplePosition(transform.position, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);
}
