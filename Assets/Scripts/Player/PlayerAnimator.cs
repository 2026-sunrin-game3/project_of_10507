using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator animator;
    EntityStat stat;

    public float direction;

    void Start()
    {
        animator = GetComponent<Animator>();
        stat = GetComponent<EntityStat>();
    }

    public void SetMoving(bool val, Vector2 axis)
    {
        animator.SetBool("isMoving", val);

        float moveRate = stat.GetResultValue("moveSpeed") / stat.GetBaseValue("moveSpeed");

        animator.SetFloat("moveSpeed", moveRate);

        if (val)
        {
            if (axis.x > 0) direction = 1;
            else if (axis.x < 0) direction = -1;

            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y);
        }
    }

    public void Jump()
    {
        animator.SetTrigger("Jump");
    }

    public void Play(string id)
    {
        if (animator == null) return;

        int stateHash = Animator.StringToHash(id);
        bool hasState = false;

        // Base Layer, Attack Layer 등 Animator에 등록된 모든 레이어를 순회하며 검사
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, stateHash))
            {
                hasState = true;
                animator.Play(id, i); // 발견된 해당 레이어에서 즉시 애니메이션 재생
                break;
            }
        }

        // 전체 레이어를 다 뒤졌는데도 없는 경우에만 경고 출력
        if (!hasState)
        {
            Debug.LogWarning($"[PlayerAnimator] Animator의 어떤 레이어에도 '{id}' State가 존재하지 않습니다. 노드 이름이나 공백을 확인해 주세요.");
        }
    }
}