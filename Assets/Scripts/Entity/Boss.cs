using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss : Enemy
{
    [SerializeField] PlayerController player;
    [SerializeField] LayerMask playerMask; // 플레이어 감지용 레이어 마스크

    public float attackDist = 1.5f;
    [SerializeField] AttackRange defaultAttack;

    public float dashRange = 5f;
    public float dashPower = 12f;
    public float dashDuration = 0.4f;
    public float dashCoolTime = 3f;
    [SerializeField] AttackRange dashAttack;
    float dashCool;

    public float jumpPower = 7f;
    public float fallSpeed = 25f;
    public float jumpCoolTime = 6f;
    [SerializeField] AttackRange jumpAttack;
    float jumpCool;

    // ----- [추가] 어퍼컷 패턴 변수 -----
    [Header("Uppercut Pattern")]
    public float uppercutRange = 2.5f;
    public float uppercutCoolTime = 6f;
    public float uppercutMovePower = 4f;
    public float uppercutKnockupPower = 9f;
    public float uppercutStunDuration = 1.2f;
    [SerializeField] AttackRange uppercutAttack;
    float uppercutCool;
    // ----------------------------------

    public float retreatTime = 0.6f;
    float retreatTimer;

    bool inPattern;
    [SerializeField] Slider bossbar;

    BossAnimator bossAnimator;

    [Header("Behavior Range (X: Min, Y: Max)")]
    public Vector2 moveTimeRange = new Vector2(1f, 2f);
    public Vector2 idleTimeRange = new Vector2(1f, 2f);

    Coroutine aiRoutine;

    void Start()
    {
        bossAnimator = GetComponent<BossAnimator>();

        dashCool = dashCoolTime;
        jumpCool = jumpCoolTime;
        uppercutCool = uppercutCoolTime;

        health.OnDeath(OnDeath);

        aiRoutine = StartCoroutine(BossAIRoutine());
    }

    void OnDeath(EntityHealth.Context ctx)
    {
        bossbar.value = health.health / health.maxHealth;
        if (aiRoutine != null) StopCoroutine(aiRoutine);
    }

    protected override void MobUpdate()
    {
        bossbar.value = health.health / health.maxHealth;
        if (dashCool > 0) dashCool -= Time.deltaTime;
        if (jumpCool > 0) jumpCool -= Time.deltaTime;
        if (uppercutCool > 0) uppercutCool -= Time.deltaTime; // 어퍼컷 쿨타임 감쇠
        if (retreatTimer > 0) retreatTimer -= Time.deltaTime;

        if (inPattern || retreatTimer > 0)
        {
            if (retreatTimer > 0)
            {
                float away = player.transform.position.x > transform.position.x ? -1 : 1;
                Move(Vector2.right * away);
                if (bossAnimator != null)
                {
                    bossAnimator.SetDirection(away);
                    bossAnimator.SetMoving(true);
                }
            }
        }
    }

    IEnumerator BossAIRoutine()
    {
        while (true)
        {
            if (inPattern || retreatTimer > 0)
            {
                yield return null;
                continue;
            }

            float dist = Vector2.Distance(player.transform.position, transform.position);

            // 1. 기본 공격 최우선
            if (dist <= attackDist)
            {
                SetVelocity(Vector2.zero);
                float dir = player.transform.position.x > transform.position.x ? 1 : -1;
                if (bossAnimator != null)
                {
                    bossAnimator.SetDirection(dir);
                    bossAnimator.SetMoving(false);
                }

                if (atkCool <= 0)
                {
                    if (bossAnimator != null) bossAnimator.Play("Attack");
                    Attack(0.5f, defaultAttack, transform.position);
                    retreatTimer = retreatTime;
                }

                yield return null;
                continue;
            }

            // 2. [추가] 어퍼컷 패턴 검사
            if (dist <= uppercutRange && uppercutCool <= 0)
            {
                uppercutCool = uppercutCoolTime;
                yield return UppercutPattern();
                continue;
            }

            // 3. 대시 패턴 검사
            if (dist <= dashRange && dashCool <= 0)
            {
                dashCool = dashCoolTime;
                yield return DashAttackPattern();
                continue;
            }

            // 4. 점프 패턴 검사
            if (dist > dashRange && jumpCool <= 0)
            {
                jumpCool = jumpCoolTime;
                yield return JumpAttackPattern();
                continue;
            }

            // 5. 기본 1~2초 이동 후 1~2초 대기
            float moveTime = Random.Range(moveTimeRange.x, moveTimeRange.y);
            float timer = 0f;

            while (timer < moveTime && !inPattern && retreatTimer <= 0)
            {
                if (Vector2.Distance(player.transform.position, transform.position) <= attackDist)
                    break;

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

            if (Vector2.Distance(player.transform.position, transform.position) > attackDist)
            {
                yield return StopAndWait();
            }
        }
    }

    // ----- [신규 추가] 어퍼컷 패턴 구현 코루틴 -----
    IEnumerator UppercutPattern()
    {
        inPattern = true;
        direction = player.transform.position.x > transform.position.x ? 1 : -1;

        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(direction);
            bossAnimator.Play("Uppercut");
        }

        // 앞쪽으로 약간 이동하면서 어퍼컷 준비
        SetVelocity(Vector2.right * direction * uppercutMovePower);

        yield return new WaitForSeconds(0.2f); // 전진하는 시간

        SetVelocity(Vector2.zero); // 타격 시점엔 정지

        // 어퍼컷 피격 판정
        Vector2 attackPos = (Vector2)transform.position + new Vector2(uppercutAttack.offset.x * direction, uppercutAttack.offset.y);
        Collider2D[] hitPlayers = Physics2D.OverlapBoxAll(attackPos, uppercutAttack.size, 0, playerMask);

        foreach (var col in hitPlayers)
        {
            PlayerMovement pMove = col.GetComponent<PlayerMovement>();
            EntityHealth pHealth = col.GetComponent<EntityHealth>();

            // 데미지 적용
            if (pHealth != null)
            {
                float dmg = stat != null ? stat.GetResultValue("attackDamage") * 1.5f : 10f;
                pHealth.GetDamage(dmg, health);
            }

            // 플레이어 스턴 및 위로 띄우기 적용
            if (pMove != null)
            {
                pMove.ApplyStun(uppercutStunDuration, uppercutKnockupPower);
            }
        }

        yield return new WaitForSeconds(0.4f); // 후딜레이

        inPattern = false;
        yield return StopAndWait();
    }

    IEnumerator StopAndWait()
    {
        SetVelocity(Vector2.zero);
        if (bossAnimator != null) bossAnimator.SetMoving(false);

        float idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        float timer = 0f;

        while (timer < idleTime && !inPattern && retreatTimer <= 0)
        {
            if (Vector2.Distance(player.transform.position, transform.position) <= attackDist)
                break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DashAttackPattern()
    {
        inPattern = true;
        direction = player.transform.position.x > transform.position.x ? 1 : -1;

        if (bossAnimator != null)
        {
            bossAnimator.SetDirection(direction);
            bossAnimator.Play("Dash");
        }

        SetVelocity(Vector2.right * direction * dashPower);

        float t = 0f;
        while (t < dashDuration && Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist)
        {
            t += Time.deltaTime;
            yield return null;
        }

        SetVelocity(Vector2.zero);
        Attack(0f, dashAttack, transform.position);

        inPattern = false;
        yield return StopAndWait();
    }

    IEnumerator JumpAttackPattern()
    {
        inPattern = true;

        if (bossAnimator != null) bossAnimator.Play("Jump");

        SetVelocity(Vector2.up * jumpPower);

        yield return new WaitForSeconds(0.2f);

        while (!OnGround())
        {
            if (Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist)
            {
                direction = player.transform.position.x > transform.position.x ? 1 : -1;
                Move(Vector2.right * direction);

                if (bossAnimator != null) bossAnimator.SetDirection(direction);
            }

            if (rigid.linearVelocity.y < 0)
                SetVelocity(new Vector2(rigid.linearVelocity.x, -fallSpeed));

            yield return null;
        }

        Attack(0f, jumpAttack, transform.position);

        inPattern = false;
        yield return StopAndWait();
    }

    protected override void DrawGizmos()
    {
        Draw(defaultAttack);
        Draw(dashAttack);
        Draw(jumpAttack);
        Draw(uppercutAttack); // 어퍼컷 기즈모 시각화 추가
    }
}