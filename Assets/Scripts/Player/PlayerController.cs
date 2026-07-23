using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EntityHealth))]
public class PlayerController : MonoBehaviour
{
    public PlayerInput input;
    public PlayerMovement movement;
    public PlayerAnimator animator;
    private PlayerBattle battle; // 🔥 PlayerBattle 참조 추가

    // 피격 연출에 필요한 컴포넌트 참조
    private EntityHealth health;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;

    [Header("Hit Feedback Settings")]
    public float knockbackForce = 4f;     // 수평 밀려나는 힘
    public float knockbackUpForce = 1.5f;  // 수직 살짝 뜨는 힘
    public float blinkDuration = 0.6f;    // 전체 깜빡이는 시간
    public int blinkCount = 5;            // 깜빡이는 횟수

    [Header("Camera Shake Settings")]
    public float shakeDuration = 0.2f;    // 카메라 흔들림 시간
    public float shakeMagnitude = 0.3f;   // 카메라 흔들림 세기

    private bool isBlinking = false;

    void Start()
    {
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<PlayerAnimator>();
        battle = GetComponent<PlayerBattle>(); // 🔥 참조 연결
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        health = GetComponent<EntityHealth>();

        if (health != null)
        {
            health.OnDamage(OnHit);
        }
    }

    private void OnHit(EntityHealth.Context ctx)
    {
        // 🔥 피격이 취소되었거나(ctx.canceled), 무적 상태(IsInvincible)일 경우 모든 피격 연출(넉백, 깜빡임, 카메라 흔들림) 무시
        if (ctx.canceled || (battle != null && battle.IsInvincible))
        {
            return;
        }

        // 1. 넉백 처리
        Vector2 attackerPos = ctx.attacker != null ? (Vector2)ctx.attacker.transform.position : transform.position;
        float hitDir = transform.position.x >= attackerPos.x ? 1f : -1f;

        rigid.linearVelocity = Vector2.zero;
        rigid.AddForce(new Vector2(hitDir * knockbackForce, knockbackUpForce), ForceMode2D.Impulse);

        // 2. 깜빡임 연출
        if (!isBlinking && spriteRenderer != null)
        {
            StartCoroutine(BlinkRoutine());
        }

        // 3. 카메라 흔들림 연출
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;
        Color originalColor = spriteRenderer.color;
        float interval = blinkDuration / (blinkCount * 2);

        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.2f);
            yield return new WaitForSeconds(interval);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(interval);
        }

        spriteRenderer.color = originalColor;
        isBlinking = false;
    }

    void Update()
    {
        // 스턴 상태일 때는 조작 및 이동 애니메이션 차단
        if (movement.isStunned) return;

        movement.Move(input.axis);
        animator.SetMoving(input.HasAxis(), input.axis);
    }
}