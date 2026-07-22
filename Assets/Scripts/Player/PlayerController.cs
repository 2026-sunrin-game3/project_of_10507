using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public PlayerInput input;
    public PlayerMovement movement;
    public PlayerAnimator animator;

    void Start()
    {
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        // 스턴 상태일 때는 조작 및 이동 애니메이션 차단
        if (movement.isStunned) return;

        movement.Move(input.axis);

        animator.SetMoving(input.HasAxis(), input.axis);
    }
}