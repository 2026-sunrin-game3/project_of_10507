using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    Animator animator;

    public float direction = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // 이동 애니메이션 설정 (Base Layer)
    public void SetMoving(bool val)
    {
        if (animator != null)
            animator.SetBool("isMoving", val);
    }

    // 바라보는 방향 전환 (scale 반전)
    public void SetDirection(float dir)
    {
        if (dir == 0) return;
        direction = dir > 0 ? 1 : -1;

        transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y);
    }

    // 🔥 특정 공격 애니메이션 재생 (Attack 레이어 Weight = 1 로 켬)
    public void Play(string id)
    {
        if (animator != null)
        {
            if (animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 1f); // Attack 레이어 활성화
                animator.Play(id, 1, 0f);       // Attack 레이어에서 재생
            }
            else
            {
                animator.Play(id, 0, 0f);
            }
        }
    }

    // 🔥 [핵심] 공격 종료 시 Attack 레이어 Weight = 0 으로 끄고 Reset!
    public void ResetAttack()
    {
        if (animator != null)
        {
            if (animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 0f); // Attack 레이어 비활성화 (Base Layer가 즉시 제어)
                animator.Play("Idle", 1, 0f);
            }
            else
            {
                animator.Play("Idle", 0, 0f);
            }
        }
    }
}