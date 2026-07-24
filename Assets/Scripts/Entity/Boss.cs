using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss : Enemy
{
    [SerializeField] PlayerController player;
    [SerializeField] LayerMask playerMask;

    [Header("Death Sequence")]
    [SerializeField] BossDeathEffect deathEffect;

    public float attackDist = 1.5f;
    [SerializeField] AttackRange defaultAttack;

    public float dashRange = 5f;
    public float dashPower = 12f;
    public float dashDuration = 0.4f;
    public float dashCoolTime = 3f;
    [SerializeField] AttackRange dashAttack;
    float dashCool;

    public float jumpPower = 9f;
    public float fallSpeed = 25f;
    public float jumpCoolTime = 6f;
    [SerializeField] AttackRange jumpAttack;
    float jumpCool;

    [Header("Uppercut Pattern")]
    public float uppercutRange = 2.5f;
    public float uppercutCoolTime = 6f;
    public float uppercutMovePower = 4f;
    public float uppercutKnockupPower = 9f;
    public float uppercutStunDuration = 1.5f;
    [SerializeField] AttackRange uppercutAttack;
    float uppercutCool;

    public float retreatTime = 0.6f;

    bool inPattern;
    public bool isStunned;

    [Header("Stun Vibration Settings")]
    [SerializeField] private Transform visualTransform;
    public float stunShakeAmount = 0.12f;
    public float stunShakeSpeed = 45f;

    // ----- [UI 및 게이지 관련] -----
    [Header("UI Settings")]
    [SerializeField] Slider bossbar;            // HP 바
    [SerializeField] Slider stunbar;            // 스턴 게이지 바

    // ----- [15타 스턴 게이지 시스템] -----
    [Header("Stun Gauge System")]
    public int maxStunGauge = 15;               // 15번 맞으면 대형 스턴 발동!
    private int currentStunGauge;
    public float bigStunDuration = 5.0f;        // 대형 스턴 지속시간 (5초)

    [Header("Stun Damage Multiplier")]
    [SerializeField] private float stunDamageMultiplier = 1.5f; // 스턴 중 데미지 50% 증가

    [Header("Big Stun VFX")]
    [SerializeField] private GameObject bigStunVFXPrefab;
    [SerializeField] private Vector2 bigStunVFXOffset = Vector2.zero;

    BossAnimator bossAnimator;

    [Header("Behavior Range (X: Min, Y: Max)")]
    public Vector2 moveTimeRange = new Vector2(1f, 2f);
    public Vector2 idleTimeRange = new Vector2(1f, 2f);

    Coroutine aiRoutine;

    void Start()
    {
        bossAnimator = GetComponent<BossAnimator>();

        if (playerMask.value == 0)
        {
            playerMask = LayerMask.GetMask("player");
        }

        if (visualTransform == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) visualTransform = sr.transform;
            else visualTransform = transform;
        }

        dashCool = 1f;
        jumpCool = 2f;
        uppercutCool = 1.5f;

        currentStunGauge = maxStunGauge;
        if (stunbar != null)
        {
            stunbar.maxValue = maxStunGauge;
            stunbar.value = currentStunGauge;
        }

        aiRoutine = StartCoroutine(BossAIRoutine());
    }

    // 🔥 EntityHealth.cs에서 데미지 인디케이터를 1회 출력하므로,
    // 보스에서는 데미지 배율 조정 및 스턴 게이지 연산만 수행합니다. (base.OnHurt 제거)
    protected override void OnHurt(EntityHealth.Context ctx)
    {
        if (ctx.canceled || (health != null && health.isDeath)) return;

        // 1. 스턴 상태일 때 데미지를 1.5배로 변경
        if (isStunned)
        {
            ctx.damage *= stunDamageMultiplier;
        }
        else
        {
            currentStunGauge--;

            if (stunbar != null)
            {
                stunbar.value = currentStunGauge;
            }

            if (currentStunGauge <= 0)
            {
                TriggerBigStun();
            }
        }
    }

    protected override void MobUpdate()
    {
        if (bossbar != null) bossbar.value = health.health / health.maxHealth;
        if (dashCool > 0) dashCool -= Time.deltaTime;
        if (jumpCool > 0) jumpCool -= Time.deltaTime;
        if (uppercutCool > 0) uppercutCool -= Time.deltaTime;
        if (atkCool > 0) atkCool -= Time.deltaTime;
    }

    public void Stun(float duration)
    {
        if (health != null && health.isDeath) return;
        StopAllCoroutines();
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        inPattern = false;

        SetVelocity(Vector2.zero);

        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
            bossAnimator.Play("Idle");
        }

        Vector3 originalLocalPos = visualTransform != null ? visualTransform.localPosition : Vector3.zero;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (visualTransform != null)
            {
                float offsetX = Mathf.Sin(timer * stunShakeSpeed) * stunShakeAmount;
                visualTransform.localPosition = originalLocalPos + new Vector3(offsetX, 0f, 0f);
            }
            yield return null;
        }

        if (visualTransform != null)
        {
            visualTransform.localPosition = originalLocalPos;
        }

        isStunned = false;
        aiRoutine = StartCoroutine(BossAIRoutine());
    }

    private void TriggerBigStun()
    {
        if (health != null && health.isDeath) return;

        StopAllCoroutines();
        StartCoroutine(BigStunRoutine());
    }

    private IEnumerator BigStunRoutine()
    {
        isStunned = true;
        inPattern = false;

        SetVelocity(Vector2.zero);

        if (bigStunVFXPrefab != null)
        {
            Vector3 vfxPos = transform.position + (Vector3)bigStunVFXOffset;
            Instantiate(bigStunVFXPrefab, vfxPos, Quaternion.identity);
        }

        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
            bossAnimator.Play("Stun");
        }

        Vector3 originalLocalPos = visualTransform != null ? visualTransform.localPosition : Vector3.zero;
        float timer = 0f;

        while (timer < bigStunDuration)
        {
            timer += Time.deltaTime;

            if (visualTransform != null)
            {
                float offsetX = Mathf.Sin(timer * stunShakeSpeed) * stunShakeAmount;
                visualTransform.localPosition = originalLocalPos + new Vector3(offsetX, 0f, 0f);
            }

            yield return null;
        }

        if (visualTransform != null)
        {
            visualTransform.localPosition = originalLocalPos;
        }

        currentStunGauge = maxStunGauge;
        if (stunbar != null)
        {
            stunbar.value = currentStunGauge;
        }

        isStunned = false;

        if (bossAnimator != null)
        {
            bossAnimator.Play("Idle");
        }

        aiRoutine = StartCoroutine(BossAIRoutine());
    }

    IEnumerator BossAIRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (isStunned || inPattern)
            {
                yield return null;
                continue;
            }

            float dist = Vector2.Distance(player.transform.position, transform.position);

            if (dist <= uppercutRange && uppercutCool <= 0)
            {
                uppercutCool = uppercutCoolTime;
                yield return UppercutPattern();
                continue;
            }

            if (dist <= attackDist && atkCool <= 0)
            {
                yield return NormalAttackPattern();
                continue;
            }

            if (dist <= dashRange && dashCool <= 0)
            {
                dashCool = dashCoolTime;
                yield return DashAttackPattern();
                continue;
            }

            if (dist >= 3.0f && jumpCool <= 0)
            {
                jumpCool = jumpCoolTime;
                yield return JumpAttackPattern();
                continue;
            }

            float moveTime = Random.Range(moveTimeRange.x, moveTimeRange.y);
            float timer = 0f;

            while (timer < moveTime && !inPattern && !isStunned)
            {
                float currentDist = Vector2.Distance(player.transform.position, transform.position);

                if ((currentDist <= uppercutRange && uppercutCool <= 0) ||
                    (currentDist <= attackDist && atkCool <= 0) ||
                    (currentDist <= dashRange && dashCool <= 0) ||
                    (currentDist >= 3.0f && jumpCool <= 0))
                {
                    break;
                }

                Chase(player.transform);
                float dir = player.transform.position.x > transform.position.x ? 1 : -1;
                if (bossAnimator != null)
                {
                    bossAnimator.SetDirection(dir);
                    bossAnimator.SetMoving(true);
                }

                timer += Time.deltaTime;
                yield return null;
            }

            SetVelocity(Vector2.zero);
            if (bossAnimator != null)
            {
                bossAnimator.SetMoving(false);
                bossAnimator.ResetAttack();
            }

            float checkDist = Vector2.Distance(player.transform.position, transform.position);
            bool canDoAnyPattern = (checkDist <= uppercutRange && uppercutCool <= 0) ||
                                   (checkDist <= attackDist && atkCool <= 0) ||
                                   (checkDist <= dashRange && dashCool <= 0) ||
                                   (checkDist >= 3.0f && jumpCool <= 0);

            if (!canDoAnyPattern && !isStunned)
            {
                yield return StopAndWait(false);
            }
        }
    }

    bool ExecuteHitCheck(AttackRange range, float damageMultiplier, string attackName, bool applyStun = false)
    {
        float dir = direction != 0 ? direction : 1f;
        Vector2 attackPos = (Vector2)transform.position + new Vector2(range.offset.x * dir, range.offset.y);
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPos, range.size, 0, playerMask);

        bool hit = false;
        foreach (var col in hitPlayers)
        {
            EntityHealth pHealth = col.GetComponent<EntityHealth>();
            PlayerMovement pMove = col.GetComponent<PlayerMovement>();

            if (pHealth != null)
            {
                float dmg = stat != null ? stat.GetResultValue("attackDamage") * damageMultiplier : 10f * damageMultiplier;
                pHealth.GetDamage(dmg, health);
                hit = true;
            }

            if (applyStun && pMove != null)
            {
                pMove.ApplyStun(uppercutStunDuration, uppercutKnockupPower);
            }
        }

        return hit;
    }

    IEnumerator NormalAttackPattern()
    {
        inPattern = true;
        atkCool = 1.5f;

        SetVelocity(Vector2.zero);
        direction = player.transform.position.x > transform.position.x ? 1 : -1;
        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(direction);
            bossAnimator.SetMoving(false);
            bossAnimator.Play("Attack");
        }

        SoundManager.Instance?.PlaySFX(5);

        yield return new WaitForSeconds(0.2f);
        ExecuteHitCheck(defaultAttack, 1.0f, "기본 공격");
        yield return new WaitForSeconds(0.2f);

        if (bossAnimator != null) bossAnimator.ResetAttack();

        float away = player.transform.position.x > transform.position.x ? -1 : 1;
        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(-away);
            bossAnimator.SetMoving(true);
        }

        float retreatTimer = 0f;
        while (retreatTimer < retreatTime && !isStunned)
        {
            Move(Vector2.right * away);
            retreatTimer += Time.deltaTime;
            yield return null;
        }

        SetVelocity(Vector2.zero);
        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
        }

        inPattern = false;
        yield return StopAndWait(true);
    }

    IEnumerator UppercutPattern()
    {
        inPattern = true;
        atkCool = 1.5f;

        direction = player.transform.position.x > transform.position.x ? 1 : -1;

        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(direction);
            bossAnimator.Play("Uppercut");
        }

        SoundManager.Instance?.PlaySFX(0);

        SetVelocity(Vector2.right * direction * uppercutMovePower);
        yield return new WaitForSeconds(0.2f);
        SetVelocity(Vector2.zero);

        ExecuteHitCheck(uppercutAttack, 1.5f, "어퍼컷", applyStun: true);

        yield return new WaitForSeconds(0.4f);

        if (bossAnimator != null) bossAnimator.ResetAttack();

        float away = player.transform.position.x > transform.position.x ? -1 : 1;
        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(-away);
            bossAnimator.SetMoving(true);
        }

        float retreatTimer = 0f;
        while (retreatTimer < retreatTime && !isStunned)
        {
            Move(Vector2.right * away);
            retreatTimer += Time.deltaTime;
            yield return null;
        }

        SetVelocity(Vector2.zero);
        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
        }

        inPattern = false;
        yield return StopAndWait(true);
    }

    IEnumerator DashAttackPattern()
    {
        inPattern = true;
        atkCool = 1.5f;

        direction = player.transform.position.x > transform.position.x ? 1 : -1;

        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(direction);
            bossAnimator.Play("Dash");
        }

        SoundManager.Instance?.PlaySFX(1);

        SetVelocity(Vector2.right * direction * dashPower);

        float t = 0f;
        while (t < dashDuration && Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist && !isStunned)
        {
            t += Time.deltaTime;
            yield return null;
        }

        SetVelocity(Vector2.zero);

        if (!isStunned)
        {
            ExecuteHitCheck(dashAttack, 1.2f, "대시 공격");
        }

        yield return new WaitForSeconds(0.3f);

        if (bossAnimator != null) bossAnimator.ResetAttack();

        inPattern = false;
        yield return StopAndWait(true);
    }

    IEnumerator JumpAttackPattern()
    {
        inPattern = true;
        atkCool = 1.5f;

        if (bossAnimator != null) bossAnimator.Play("Jump");

        SetVelocity(Vector2.up * jumpPower);

        yield return new WaitForSeconds(0.15f);

        while (!OnGround() && !isStunned)
        {
            float currentDistX = player.transform.position.x - transform.position.x;
            direction = currentDistX > 0 ? 1 : -1;

            if (bossAnimator != null) bossAnimator.SetDirection(direction);

            float jumpXSpeed = Mathf.Clamp(currentDistX * 4f, -14f, 14f);
            float currentVy = rigid.linearVelocity.y < 0 ? -fallSpeed : rigid.linearVelocity.y;

            SetVelocity(new Vector2(jumpXSpeed, currentVy));
            yield return null;
        }

        SetVelocity(Vector2.zero);

        if (!isStunned)
        {
            ExecuteHitCheck(jumpAttack, 1.5f, "점프 공격");
        }

        yield return new WaitForSeconds(0.3f);

        if (bossAnimator != null) bossAnimator.ResetAttack();

        inPattern = false;
        yield return StopAndWait(true);
    }

    IEnumerator StopAndWait(bool forceWait = false)
    {
        SetVelocity(Vector2.zero);
        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
        }

        float idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        float timer = 0f;

        while (timer < idleTime && !inPattern && !isStunned)
        {
            if (!forceWait && Vector2.Distance(player.transform.position, transform.position) <= attackDist)
                break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    protected override void DrawGizmos()
    {
        DrawAttackGizmo(defaultAttack, Color.red);
        DrawAttackGizmo(dashAttack, Color.yellow);
        DrawAttackGizmo(jumpAttack, Color.green);
        DrawAttackGizmo(uppercutAttack, Color.cyan);
    }

    void DrawAttackGizmo(AttackRange range, Color color)
    {
        if (!range.drawGizmos) return;
        Gizmos.color = color;
        float dir = direction != 0 ? direction : 1f;
        Vector2 pos = (Vector2)transform.position + new Vector2(range.offset.x * dir, range.offset.y);
        Gizmos.DrawWireCube(pos, range.size);
    }

    protected override void OnDeath(EntityHealth.Context ctx)
    {
        StopAllCoroutines();
        isStunned = true;
        inPattern = false;

        SetVelocity(Vector2.zero);

        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(false);
            bossAnimator.ResetAttack();
        }

        if (deathEffect != null)
        {
            deathEffect.PlayDeathSequence(); // 🔥 페이드아웃 → 이미지 표시 후 자체적으로 Destroy 처리
        }
    }
}