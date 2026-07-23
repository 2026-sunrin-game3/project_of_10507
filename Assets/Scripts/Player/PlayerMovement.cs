using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rigid;
    private EntityStat stat;
    private PlayerAnimator animator; // 🔥 플레이어 애니메이터 참조 추가

    public float jumpPower = 12f;

    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;

    // ----- [스턴 상태 및 어퍼컷 전용 진동 설정] -----
    public bool isStunned;

    [Header("Uppercut Hit Vibration Settings")]
    [SerializeField] private Transform visualTransform;
    public float stunShakeAmount = 0.1f;
    public float stunShakeSpeed = 45f;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        stat = GetComponent<EntityStat>();
        animator = GetComponent<PlayerAnimator>(); // 🔥 참조 자동 연결

        if (visualTransform == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                if (sr.transform != transform)
                {
                    visualTransform = sr.transform;
                    break;
                }
            }
        }
    }

    public void Move(Vector2 axis)
    {
        if (isStunned) return;
        float moveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moveSpeed * Time.deltaTime);
    }

    public void SetVelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }

    // 🔥 보스 어퍼컷 피격 시 호출되는 스턴, 띄우기 및 1.5초 진동 루틴
    public void ApplyStun(float duration, float knockupPower)
    {
        StartCoroutine(StunRoutine(duration, knockupPower));
    }

    private IEnumerator StunRoutine(float duration, float knockupPower)
    {
        isStunned = true;

        // 🔥 1. 피격/스턴 애니메이션 직접 재생
        if (animator != null)
        {
            animator.Play("Stun"); // Animator Controller에 등록된 State 이름
        }

        // 2. 수직 도약 (Y축 속도 리셋 후 Impulse 힘으로 확실히 띄움)
        if (knockupPower > 0 && rigid != null)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
            rigid.AddForce(Vector2.up * knockupPower, ForceMode2D.Impulse);
        }

        // 3. 1.5초간 좌우 진동
        bool canShake = visualTransform != null && visualTransform != transform;
        Vector3 originalLocalPos = canShake ? visualTransform.localPosition : Vector3.zero;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (canShake)
            {
                float offsetX = Mathf.Sin(timer * stunShakeSpeed) * stunShakeAmount;
                visualTransform.localPosition = originalLocalPos + new Vector3(offsetX, 0f, 0f);
            }

            yield return null;
        }

        // 진동 원위치 복구 및 스턴 해제
        if (canShake)
        {
            visualTransform.localPosition = originalLocalPos;
        }

        isStunned = false;

        // 🔥 4. 스턴 해제 시 대기(Idle) 애니메이션 상태로 복귀
        if (animator != null)
        {
            animator.SetMoving(false, Vector2.zero);
        }
    }

    public bool OnGround()
    {
        Vector2 center = transform.position + Vector3.down * groundDist_ * 0.5f;
        Vector2 size = new Vector3(0.3f, groundDist_);
        Collider2D[] cast = Physics2D.OverlapBoxAll(center, size, 0f, groundMask_);

        return cast.Length > 0;
    }

    public bool Jump()
    {
        if (isStunned) return false;

        if (OnGround())
        {
            SetVelocity(Vector2.up * jumpPower);
            return true;
        }

        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.down * groundDist_ * 0.5f, new Vector3(0.3f, groundDist_));
    }
}