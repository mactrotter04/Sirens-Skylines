using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum OffMeshLinkMoveMethod
{
    Teleport,
    NormalSpeed,
    Parabola,
    Curve
}

[RequireComponent(typeof(NavMeshAgent))]
public class AgentLinkMover : MonoBehaviour
{
    public OffMeshLinkMoveMethod m_Method = OffMeshLinkMoveMethod.Parabola;
    public AnimationCurve m_Curve = new AnimationCurve();

    [SerializeField] float windupTime = 0.8f;
    [SerializeField] float jumpHeight = 2.0f;
    [SerializeField] float jumpDuration = 0.5f;
    [SerializeField] float windupTurnDegreesPerSecond = 720f;
    [SerializeField] Animator animator;

    IEnumerator Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                yield return StartCoroutine(Windup(agent));

                if (m_Method == OffMeshLinkMoveMethod.NormalSpeed)
                    yield return StartCoroutine(NormalSpeed(agent));
                else if (m_Method == OffMeshLinkMoveMethod.Parabola)
                    yield return StartCoroutine(Parabola(agent, jumpHeight, jumpDuration));
                else if (m_Method == OffMeshLinkMoveMethod.Curve)
                    yield return StartCoroutine(Curve(agent, jumpDuration));

                if (animator != null)
                {
                    animator.ResetTrigger("Jump");
                    animator.SetTrigger("Grounded");

                }
                agent.CompleteOffMeshLink();
            }
            yield return null;
        }
    }

    IEnumerator NormalSpeed(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        while (agent.transform.position != endPos)
        {
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }

    IEnumerator Curve(NavMeshAgent agent, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        Vector3 travelFlat = endPos - startPos;
        travelFlat.y = 0f;

        bool hasTravelDirection = travelFlat.sqrMagnitude > 0.0001f;

        Quaternion travelRotation = agent.transform.rotation;

        if(hasTravelDirection)
        {
            travelRotation = Quaternion.LookRotation(travelFlat);
        }

        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = m_Curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;

            if(hasTravelDirection)
            {
                agent.transform.rotation = travelRotation;
            }

            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }

    IEnumerator Windup(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.ResetTrigger("Grounded");
            animator.Play("JumpStart", 0, 0f);
            animator.Update(0f);
        }

        float elapsed = 0f;
        bool frozen = false;

        while(elapsed < windupTime)
        {
            Vector3 flat = endPos - agent.transform.position;
            flat.y = 0f;

            if(flat.sqrMagnitude < 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(flat);
                agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, target, windupTurnDegreesPerSecond * Time.deltaTime);
            }

            if(!frozen && animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

                if(state.IsName("JumpStart") && state.normalizedTime >= 0.95f)
                {
                    animator.speed = 0f;
                    frozen = true;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if(animator != null)
        {
            animator.speed = 1f;
        }

        Vector3 finalFlat = endPos - agent.transform.position;
        finalFlat.y = 0f;

        if(finalFlat.sqrMagnitude > 0.0001f)
        {
            agent.transform.rotation = Quaternion.LookRotation(finalFlat);
        }////////////////////// hamburger
    }
}