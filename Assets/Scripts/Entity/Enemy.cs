using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public EntityHealth health;
    public EntityStat stat;
    public Rigidbody2D rigid;
    public float direction;
    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;
    public float atkCool;
    [SerializeField] LayerMask enemyMask;

    protected virtual void Awake()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<EntityStat>();
        rigid = GetComponent<Rigidbody2D>();

        if (health != null)
        {
            health.OnDamage(OnHurt); // 🔥 유일한 OnDamage 이벤트 등록 위치
            health.OnDeath(OnDeath);
        }
    }

    protected virtual void OnDeath(EntityHealth.Context ctx)
    {
        Destroy(gameObject);
    }

    // 🔥 EntityHealth.cs가 인디케이터를 1회 출력하므로, 여기서는 추가 생성 코드를 제거합니다.
    // 자식 클래스(Boss 등)에서 피격 시 수치 연산이나 연출이 필요할 때 override할 수 있습니다.
    protected virtual void OnHurt(EntityHealth.Context ctx)
    {
    }

    void Update()
    {
        if (atkCool > 0)
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);

        MobUpdate();
    }
    protected virtual void MobUpdate() { }

    public void Chase(Transform target)
    {
        if (target.position.x > transform.position.x)
        {
            Move(Vector2.right);
        }
        else if (target.position.x < transform.position.x)
        {
            Move(Vector2.left);
        }
    }

    public void Move(Vector2 axis)
    {
        float moveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moveSpeed * Time.deltaTime);
    }

    public void SetVelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }

    public void Attack(float cool, AttackRange range, Vector2 center)
    {
        if (atkCool > 0)
            return;
        atkCool = cool;

        var col = Physics2D.OverlapBoxAll(center + range.offset,
            range.size,
            0,
            enemyMask
        );

        foreach (var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(stat.GetResultValue("attackDamage"), health);
            }
        }
    }

    public bool OnGround()
    {
        Vector2 center = transform.position + Vector3.down * groundDist_ * 0.5f;
        Vector2 size = new Vector3(0.3f, groundDist_);
        Collider2D[] cast = Physics2D.OverlapBoxAll(center, size, 0f, groundMask_);

        return cast.Length > 0;
    }

    protected void Draw(AttackRange range)
    {
        if (!range.drawGizmos)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + range.offset, range.size);
    }

    void OnDrawGizmos()
    {
        DrawGizmos();
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.down * groundDist_ * 0.5f, new Vector3(0.3f, groundDist_));
    }

    protected virtual void DrawGizmos() { }
}