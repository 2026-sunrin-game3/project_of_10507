using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct AttackRange
{
    public Vector2 offset, size;
    public bool drawGizmos;
}

public class PlayerBattle : MonoBehaviour
{
    public EntityHealth health;
    public PlayerMovement movement;
    public PlayerAnimator animator;
    [SerializeField] DamageIndicator indicatorPrefab; // 🔥 프리팹 참조 - 반드시 인스펙터에서 재연결 필요
    public EntityStat stat;
    public float atkCool;

    public AttackRange defaultAttack;

    [SerializeField] LayerMask enemyMask;
    [SerializeField] float dashPower, dashTime;
    public bool isDash;
    [SerializeField] Slider healthbar;

    [Header("Skill 1 Settings")]
    [SerializeField] private float skill1CastTime = 0.8f;
    [SerializeField] private float skill1BuffDuration = 5.0f;
    [SerializeField] private GameObject buffEffectPrefab;
    [SerializeField] private Vector2 buffEffectOffset = Vector2.zero;
    [SerializeField] private bool attachBuffEffectToPlayer = true;

    [Header("Hammer Slam Settings (Skill 2)")]
    public AttackRange hammerAttack;
    [SerializeField] private float hammerDamage = 7.0f;
    [SerializeField] private float hammerSlamCastTime = 1.0f;
    [SerializeField] private float hammerForwardPower = 4.0f;
    [SerializeField] private float hammerForwardDuration = 0.15f;
    [SerializeField] private GameObject hammerEffectPrefab;
    [SerializeField] private Vector2 hammerEffectOffset = new Vector2(1.2f, -0.5f);

    [Header("Charge Skill Settings (Skill 3)")]
    public AttackRange chargeAttack;
    [SerializeField] private float chargeHitDamage = 5.0f;
    [SerializeField] private float chargeDuration = 2.0f;
    [SerializeField] private float chargeMoveSpeed = 2.5f;
    [SerializeField] private float chargeHitInterval = 0.4f;
    [SerializeField] private GameObject chargeEffectPrefab;

    [Header("Parry Skill Settings")]
    [SerializeField] private float parryDuration = 0.5f;
    [SerializeField] private float enemyStunDuration = 1.0f;
    [SerializeField] private float parryKnockbackForce = 6.0f;
    [SerializeField] private float parryHealAmount = 5.0f;
    [SerializeField] private GameObject parryStanceVFXPrefab;
    [SerializeField] private GameObject parrySuccessVFXPrefab;
    [SerializeField] private Vector2 parryVFXOffset = Vector2.zero;

    private bool isHammering = false;
    private bool isCharging = false;
    private bool isParrying = false;

    public bool IsInvincible => isDash || isHammering || isCharging || isParrying;

    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<EntityStat>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<PlayerAnimator>();

        health.OnDamage(OnHurt);
    }

    private float GetBuffRatio()
    {
        if (stat == null) return 1.0f;

        float baseAtk = stat.GetBaseValue("attackDamage");
        if (baseAtk <= 0) return 1.0f;

        float currentAtk = stat.GetResultValue("attackDamage");
        return currentAtk / baseAtk;
    }

    void OnHurt(EntityHealth.Context ctx)
    {
        if (isParrying)
        {
            ctx.canceled = true;
            OnParrySuccess(ctx.attacker);
            return;
        }

        if (IsInvincible)
        {
            ctx.canceled = true;
            return;
        }

        // 🔥 직접 인디케이터를 띄우지 않고, EntityHealth가 출력할 색상만 빨간색으로 지정합니다.
        ctx.indicatorColor = Color.red;
    }

    private void OnParrySuccess(EntityHealth attacker)
    {
        SoundManager.Instance?.PlaySFX(2);

        if (health != null)
        {
            health.health = Mathf.Min(health.maxHealth, health.health + parryHealAmount);
        }

        // 패링 성공 시 체력 회복(힐) 텍스트 - 초록색 "+5" 표기
        if (indicatorPrefab != null)
        {
            Vector3 indicatorPos = transform.position + new Vector3(0, 1.5f, 0);
            DamageIndicator healIndicator = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);
            healIndicator.Setup(parryHealAmount, Color.green, true);
        }
        else
        {
            Debug.LogWarning("[PlayerBattle] indicatorPrefab이 연결되어 있지 않아 힐 인디케이터를 표시하지 못했습니다.");
        }

        if (parrySuccessVFXPrefab != null)
        {
            Vector3 targetPos = (attacker != null) ? attacker.transform.position : transform.position;
            Instantiate(parrySuccessVFXPrefab, targetPos + (Vector3)parryVFXOffset, Quaternion.identity);
        }

        if (attacker != null)
        {
            attacker.SendMessage("Stun", enemyStunDuration, SendMessageOptions.DontRequireReceiver);

            Rigidbody2D attackerRigid = attacker.GetComponent<Rigidbody2D>();
            if (attackerRigid != null)
            {
                float pushDir = attacker.transform.position.x >= transform.position.x ? 1f : -1f;
                attackerRigid.linearVelocity = Vector2.zero;
                attackerRigid.AddForce(new Vector2(pushDir * parryKnockbackForce, 1.5f), ForceMode2D.Impulse);
            }
        }

        isParrying = false;
    }

    public void Parry()
    {
        if (movement.isStunned) return;
        StartCoroutine(Parry_());
    }

    IEnumerator Parry_()
    {
        movement.isStunned = true;
        isParrying = true;
        movement.SetVelocity(Vector2.zero);

        if (animator != null)
        {
            animator.Play("Parry");
        }

        GameObject stanceVFX = null;
        if (parryStanceVFXPrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3)parryVFXOffset;
            stanceVFX = Instantiate(parryStanceVFXPrefab, spawnPos, Quaternion.identity, transform);
        }

        float timer = 0f;
        while (timer < parryDuration && isParrying)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (stanceVFX != null)
        {
            Destroy(stanceVFX);
        }

        isParrying = false;
        movement.isStunned = false;
    }

    void Update()
    {
        if (healthbar != null && health != null)
        {
            healthbar.value = health.health / health.maxHealth;
        }

        if (atkCool > 0 && stat != null)
        {
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
        }
    }

    public void Dash(int direction)
    {
        StartCoroutine(dash_(direction));
    }

    IEnumerator dash_(int direction)
    {
        isDash = true;
        movement.SetVelocity(Vector2.right * direction * dashPower);

        yield return new WaitForSeconds(dashTime);

        movement.SetVelocity(Vector2.zero);
        isDash = false;
    }

    public void Attack()
{
    if (atkCool > 0) return;
    atkCool = 0.5f;

    SoundManager.Instance?.PlaySFX(4); // 🔥 플레이어 기본 공격 효과음

    var col = Physics2D.OverlapBoxAll((Vector2)transform.position + defaultAttack.offset, defaultAttack.size, 0, enemyMask);

    foreach (var target in col)
    {
        EntityHealth hp = target.GetComponent<EntityHealth>();

        if (hp != null)
        {
            hp.GetDamage(stat.GetResultValue("attackDamage"), health);
        }
    }
}

    public void SKill1()
    {
        if (movement.isStunned) return;
        StartCoroutine(Skill1_());
    }

    IEnumerator Skill1_()
    {
        movement.isStunned = true;
        movement.SetVelocity(Vector2.zero);

        if (animator != null)
        {
            animator.Play("Skill1");
        }

        GameObject activeBuffVFX = null;
        if (buffEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3)buffEffectOffset;

            if (attachBuffEffectToPlayer)
            {
                activeBuffVFX = Instantiate(buffEffectPrefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                activeBuffVFX = Instantiate(buffEffectPrefab, spawnPos, Quaternion.identity);
            }
        }

        if (BuffUI.Instance != null)
        {
            BuffUI.Instance.ActivateBuff(skill1BuffDuration);
        }

        var atkbuf = new EntityStat.Buf
        {
            Key = "attackDamage",
            mathType = MathType.Increase,
            Value = 60
        };
        var atkSpeedbuf = new EntityStat.Buf
        {
            Key = "attackSpeed",
            mathType = MathType.Add,
            Value = 50
        };
        stat.bufs.Add(atkbuf);
        stat.bufs.Add(atkSpeedbuf);
        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");

        yield return new WaitForSeconds(skill1CastTime);
        movement.isStunned = false;

        float remainingBuffTime = Mathf.Max(0f, skill1BuffDuration - skill1CastTime);
        yield return new WaitForSeconds(remainingBuffTime);

        stat.bufs.Remove(atkbuf);
        stat.bufs.Remove(atkSpeedbuf);
        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");

        if (activeBuffVFX != null)
        {
            Destroy(activeBuffVFX);
        }
    }

    public void HammerSlam()
    {
        if (movement.isStunned) return;
        StartCoroutine(HammerSlam_());
    }

    IEnumerator HammerSlam_()
    {
        movement.isStunned = true;
        isHammering = true;

        float dir = animator.direction != 0 ? animator.direction : Mathf.Sign(transform.localScale.x);

        if (animator != null)
        {
            animator.Play("HammerSlam");
        }

        SoundManager.Instance?.PlaySFX(3);

        movement.SetVelocity(Vector2.right * dir * hammerForwardPower);
        yield return new WaitForSeconds(hammerForwardDuration);
        movement.SetVelocity(Vector2.zero);

        if (hammerEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(hammerEffectOffset.x * dir, hammerEffectOffset.y, 0f);
            Instantiate(hammerEffectPrefab, spawnPos, Quaternion.identity);
        }

        Vector2 attackPos = (Vector2)transform.position + new Vector2(hammerAttack.offset.x * dir, hammerAttack.offset.y);
        var col = Physics2D.OverlapBoxAll(attackPos, hammerAttack.size, 0, enemyMask);

        float finalDamage = hammerDamage * GetBuffRatio();

        foreach (var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();

            if (hp != null)
            {
                hp.GetDamage(finalDamage, health);
            }
        }

        float remainingTime = Mathf.Max(0f, hammerSlamCastTime - hammerForwardDuration);
        yield return new WaitForSeconds(remainingTime);

        isHammering = false;
        movement.isStunned = false;
    }

    public void ChargeAttack()
    {
        if (movement.isStunned) return;
        StartCoroutine(ChargeAttack_());
    }

    IEnumerator ChargeAttack_()
    {
        movement.isStunned = true;
        isCharging = true;

        float dir = animator.direction != 0 ? animator.direction : Mathf.Sign(transform.localScale.x);

        if (animator != null)
        {
            animator.Play("ChargeAttack");
        }

        GameObject chargeVFX = null;
        if (chargeEffectPrefab != null)
        {
            chargeVFX = Instantiate(chargeEffectPrefab, transform.position, Quaternion.identity, transform);
        }

        float timer = 0f;
        float hitTimer = 0f;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;
            hitTimer += Time.deltaTime;

            movement.SetVelocity(Vector2.right * dir * chargeMoveSpeed);

            if (hitTimer >= chargeHitInterval)
            {
                hitTimer = 0f;
                PerformChargeHit(dir);
            }

            yield return null;
        }

        movement.SetVelocity(Vector2.zero);

        if (chargeVFX != null)
        {
            Destroy(chargeVFX);
        }

        isCharging = false;
        movement.isStunned = false;
    }

    private void PerformChargeHit(float dir)
    {
        Vector2 attackPos = (Vector2)transform.position + new Vector2(chargeAttack.offset.x * dir, chargeAttack.offset.y);
        var col = Physics2D.OverlapBoxAll(attackPos, chargeAttack.size, 0, enemyMask);

        float finalDamage = chargeHitDamage * GetBuffRatio();

        foreach (var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(finalDamage, health);
            }
        }
    }

    void Draw(AttackRange range)
    {
        if (!range.drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + range.offset, range.size);
    }

    void OnDrawGizmos()
    {
        Draw(defaultAttack);
        Draw(hammerAttack);
        Draw(chargeAttack);
    }
}