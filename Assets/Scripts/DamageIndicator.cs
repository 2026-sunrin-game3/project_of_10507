using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
    [SerializeField] Text text;
    [SerializeField] float time, floatingSclae;

    private float lifeTime = 0f;

    void Update()
    {
        lifeTime += Time.deltaTime;
        transform.Translate(Vector2.up * floatingSclae * Time.deltaTime);

        if (lifeTime > 0.65f)
        {
            Destroy(gameObject);
        }
    }

    // 🔥 isHeal이 true면 앞에 "+" 표시 (회복량 구분용)
    public void Setup(float amount, Color color, bool isHeal = false)
    {
        if (text != null)
        {
            string sign = isHeal ? "+" : "";
            text.text = sign + Mathf.Round(amount).ToString();
            text.color = color;
        }
        else
        {
            Debug.LogWarning("[DamageIndicator] text(Text 컴포넌트)가 연결되어 있지 않습니다.");
        }
    }
}