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
        movement.Move(input.axis);

        animator.SetMoving(input.HasAxis() , input.axis);
    }
}