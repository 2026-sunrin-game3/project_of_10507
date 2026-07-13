using UnityEngine;

[System.Serializable]
public struct AttackRange
{
    public Vector2 offset , size;
    public bool drawGizmos;
}

public class PlayerBattle : MonoBehaviour
{
    public EntityHealth health;
    public EntityStat stat;
    public float atkCool;

    public AttackRange defaultAttack;

    [SerializeField] LayerMask enemyMask;

    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<EntityStat>();
    }

    void Update()
    {
        if (atkCool > 0) atkCool -= Time.deltaTime * (1 + stat.GetresultValue("atkSpeed") / 100);
    }

    public void Attack()
    {
        if (atkCool > 0) return;
        atkCool = 0.5f;

        var col = Physics2D.OverlapBoxAll((Vector2)transform.position + defaultAttack.offset , defaultAttack.size , 0 , enemyMask);

        foreach (var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();

            if (hp != null)
            {
                hp.GetDamage(stat.GetresultValue("attackDamage") , health);
            }
        }
    }

    void Draw(AttackRange range)
    {
        if (!range.drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + range.offset , range.size);
    }

    void OnDrawGizmos()
    {
        Draw(defaultAttack);
    }
}
