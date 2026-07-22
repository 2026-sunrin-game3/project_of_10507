using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rigid;

    EntityStat stat;

    public float jumpPower = 12f;

    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;

    // ----- [추가] 스턴 상태 변수 -----
    public bool isStunned { get; private set; }

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        stat = GetComponent<EntityStat>();
    }

    public void Move(Vector2 axis)
    {
        if (isStunned) return; // 스턴 상태시 이동 불가
        float moveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moveSpeed * Time.deltaTime);
    }

    public void SetVelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }

    // ----- [추가] 스턴 및 띄우기(Knockup) 처리 -----
    public void ApplyStun(float duration, float knockupPower)
    {
        StartCoroutine(StunRoutine(duration, knockupPower));
    }

    private IEnumerator StunRoutine(float duration, float knockupPower)
    {
        isStunned = true;

        // 위로 띄우기
        if (knockupPower > 0)
        {
            SetVelocity(new Vector2(0, knockupPower));
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
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
        if (isStunned) return false; // 스턴 상태시 점프 불가

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