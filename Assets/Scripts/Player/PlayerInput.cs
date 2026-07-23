using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    PlayerMovement movement;
    PlayerAnimator animator;
    PlayerBattle battle;
    public Vector2 axis;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        battle = GetComponent<PlayerBattle>();
        animator = GetComponent<PlayerAnimator>();
    }

    public void OnMove(InputValue value)
    {
        if (movement != null && movement.isStunned)
        {
            axis = Vector2.zero;
            return;
        }

        Vector2 axis_ = value.Get<Vector2>();
        axis = new Vector2(axis_.x, 0);
    }

    public bool HasAxis()
    {
        return axis.x != 0 || axis.y != 0;
    }

    public void OnJump()
    {
        if (movement.isStunned) return;
        if (movement.Jump()) animator.Jump();
    }

    public void OnAttack()
    {
        if (movement.isStunned) return;
        battle.Attack();
        animator.Play("Attack1");
    }

    public void OnDash()
    {
        if (movement.isStunned) return;
        battle.Dash((int)animator.direction);
    }

    public void OnSkill1()
    {
        if (movement.isStunned) return;
        battle.SKill1();
    }

    public void OnSkill2()
    {
        if (movement.isStunned) return;
        battle.HammerSlam();
    }

    public void OnSkill3()
    {
        if (movement.isStunned) return;
        battle.ChargeAttack();
    }

    // 🔥 4번 스킬 (패링)
    public void OnSkill4()
    {
        if (movement.isStunned) return;
        battle.Parry();
    }
}