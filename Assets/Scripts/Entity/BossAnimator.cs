using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    Animator animator;
    EntityStat stat;

    public float direction = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        stat = GetComponent<EntityStat>();
    }

    // 이동 애니메이션 설정
    public void SetMoving(bool val)
    {
        if (animator != null)
            animator.SetBool("isMoving", val);
    }

    // 바라보는 방향 전환 (PlayerAnimator와 동일한 scale 방식)
    public void SetDirection(float dir)
    {
        if (dir == 0) return;
        direction = dir > 0 ? 1 : -1;

        transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y);
    }

    // 특정 애니메이션 재생 (공격, 대시, 점프 등)
    public void Play(string id)
    {
        if (animator != null)
            animator.Play(id);
    }
}