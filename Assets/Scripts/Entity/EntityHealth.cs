using System;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    public class Context
    {
        public float damage;
        public EntityHealth attacker;
        public bool canceled;
        public Color indicatorColor = Color.white;
        public bool showIndicator = true;
    }

    public float health = 100f;
    public float maxHealth = 100f;
    public bool isDeath = false;

    [SerializeField] private DamageIndicator indicatorPrefab; // 🔥 프리팹 직접 연결

    private Action<Context> onDamageCallback;
    private Action<Context> onDeathCallback;

    public void OnDamage(Action<Context> action) => onDamageCallback += action;
    public void OnDeath(Action<Context> action) => onDeathCallback += action;

    public void GetDamage(float damage, EntityHealth attacker)
    {
        if (isDeath) return;

        Context ctx = new Context
        {
            damage = damage,
            attacker = attacker,
            canceled = false,
            indicatorColor = Color.white,
            showIndicator = true
        };

        // 1. 피격/스턴/색상 지정 연산 (Player, Boss 등에서 ctx 값 수정)
        onDamageCallback?.Invoke(ctx);

        if (ctx.canceled) return;

        // 2. 실제 체력 차감
        health -= ctx.damage;
        if (health <= 0)
        {
            health = 0;
            isDeath = true;
            onDeathCallback?.Invoke(ctx);
        }

        // 3. 시스템 전체에서 오직 이 한 곳에서만 인디케이터 프리팹 소환
        if (ctx.showIndicator && indicatorPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, 1.8f, 0f);
            DamageIndicator indicator = Instantiate(indicatorPrefab, spawnPos, Quaternion.identity);
            indicator.Setup(ctx.damage, ctx.indicatorColor);
        }
    }
}