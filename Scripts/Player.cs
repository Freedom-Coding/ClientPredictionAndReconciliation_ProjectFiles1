using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerControls : NetworkBehaviour
{
    private Animator animator;
    private Vector2 currentMoveDirection;
    public NetworkVariable<int> playerScore = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        playerScore.OnValueChanged += FindObjectOfType<PlayerUI>().UpdateScoreUI;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Attack();
    }


    private bool canAttack = true;
    private void Attack()
    {
        if (Input.GetMouseButton(0))
        {
            animator.SetFloat("Attack", 1);

            if (canAttack)
            {
                AttackServerRPC(currentMoveDirection);

                StartCoroutine(AttackCooldown());
            }
        }
        else
        {
            animator.SetFloat("Attack", 0);
        }
    }

    [ServerRpc]
    private void AttackServerRPC(Vector2 currentMoveDirection)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 1f, currentMoveDirection, 0, 1 << 6);

        if (hits.Length > 0)
        {
            hits[0].transform.GetComponent<HealthSystem>().OnDamageDealt(50);
            if (hits[0].transform.GetComponent<HealthSystem>().health < 0)
            {
                playerScore.Value++;
            }
        }
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1);
        canAttack = true;
    }
}