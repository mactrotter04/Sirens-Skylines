using UnityEngine;
using UnityEngine.AI;

public class WAnder : MonoBehaviour
{
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float minIdleTime = 2f;
    [SerializeField] float wanderRadius = 100f;
    [SerializeField] float maxIdleTime = 5f;
    [SerializeField] float sampleingDistance = 1f;
    
    Animator animator;
    NavMeshAgent navMeshAgent;
    float idleTimer = 0f;
    Vector3 wanderOrigin;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Wander();
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        animator.SetFloat("MotionSpeed", 1f);
    }

    void Wander()
    {
        if (!navMeshAgent.pathPending)
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

    void OnDrawGizmosSelected()
    {
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

}
