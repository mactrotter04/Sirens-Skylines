using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] float healthPoints = 100f;
    [SerializeField] float DissapearDelay = 10f;

    bool isDead = false;

    public bool IsDead()
    {
        return isDead;
    }

    public void TakeDamage(float damage) // responsible for the enemey taking damage 
    {
        BroadcastMessage(nameof(EnemyController.OnDamageTaken)); //tells the chase script that the enemy has taken damage

        healthPoints -= damage;

        if (healthPoints <= 0) //resposible for the enemys death
        {
            Death();
        }
    }

    void Death()
    {
        if (isDead) return;
        isDead = true;
        GetComponentInChildren<Animator>().SetTrigger("Dead");
        Destroy(gameObject, DissapearDelay);
    }
}
