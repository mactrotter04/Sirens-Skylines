using Unity.VisualScripting;
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


    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float chaseSpeed = 6.5f;
 
    float distanceToTarget = Mathf.Infinity;
    bool isProvoked = false;
    float idleTimer = 0f;

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
    }

    void Engagetarget()
    {
        FaceTarget();

        if (distanceToTarget >= navMeshAgent.stoppingDistance)
        {
            Chasetarget();
        }

        if (distanceToTarget <= navMeshAgent.stoppingDistance)
        {
            AttackTarget();
        }
    }

    void Chasetarget()
    {
        navMeshAgent.speed = chaseSpeed;
        animator.SetBool("Chase", true);
        navMeshAgent.SetDestination(target.transform.position);
    }

    void AttackTarget()
    {
        animator.SetBool("Chase", false);
        animator.SetTrigger("Shoot");
    }

    void FaceTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;

        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    void Wander()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <=  navMeshAgent.stoppingDistance)
        {
            GetComponent<Animator>().SetBool("chase", false);

            idleTimer -= Time.deltaTime;

            if(idleTimer <= 0f)
            {
                if(GetNewWanderDestination(out Vector3 destination))
                {
                    GetComponent<Animator>().SetTrigger("Move");
                    navMeshAgent.speed = walkSpeed;
                    navMeshAgent.SetDestination(destination);
                }

                idleTimer = Random.Range(minIdleTime, maxIdleTime);
            }
        }
    }

    bool GetNewWanderDestination(out Vector3 result)
    {
        for(int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = wanderOrigin + Random.insideUnitSphere * wanderRadius;

            if(NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sampleingDistance, NavMesh.AllAreas))
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


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseramge);

        Gizmos.color = Color.hotPink;
        Gizmos.DrawWireSphere(wanderOrigin, wanderRadius);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(wanderOrigin, 1f); // current pos

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(navMeshAgent.destination, 1f); // destination pis 
    }

    //NavMesh.SamplePosition(transform.position, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);
}
