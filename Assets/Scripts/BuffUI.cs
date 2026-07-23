using System.Collections;
using UnityEngine;

public class BuffUI : MonoBehaviour
{
    public static BuffUI Instance { get; private set; }

    [Header("Buff UI Target")]
    [SerializeField] private CanvasGroup buffCanvasGroup; // 자식 포함 전체 UI 투명도 제어용 컴포넌트

    private Coroutine buffCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작할 때는 버프 UI 비활성화
        if (buffCanvasGroup != null)
        {
            buffCanvasGroup.gameObject.SetActive(false);
        }
    }

    // 버프 시작/갱신 호출 함수
    public void ActivateBuff(float duration)
    {
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
        }
        buffCoroutine = StartCoroutine(BuffRoutine(duration));
    }

    private IEnumerator BuffRoutine(float duration)
    {
        if (buffCanvasGroup == null) yield break;

        buffCanvasGroup.gameObject.SetActive(true);
        buffCanvasGroup.alpha = 1.0f; // 초기화 (완전 불투명)

        float timer = duration;
        float blinkTimer = 0f;
        bool isOpaque = true;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float ratio = timer / duration; // 남은 시간 비율 (1.0 ~ 0.0)

            float blinkInterval = 0f;

            // 남은 시간 비율에 따른 깜빡임 간격 설정
            if (ratio <= 0.10f)
            {
                blinkInterval = 0.07f; // 10% 이하: 매우 빠르게
            }
            else if (ratio <= 0.25f)
            {
                blinkInterval = 0.15f; // 25% 이하: 조금 더 빠르게
            }
            else if (ratio <= 0.50f)
            {
                blinkInterval = 0.35f; // 50% 이하: 천천히
            }

            // CanvasGroup의 alpha를 조절하여 자식 오브젝트 전체 투명도 동시 조절
            if (blinkInterval > 0f)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    isOpaque = !isOpaque;

                    // 1.0(불투명)과 0.2(반투명) 사이 전환
                    buffCanvasGroup.alpha = isOpaque ? 1.0f : 0.2f;
                }
            }
            else
            {
                // 50% 초과일 때는 완전 불투명 유지
                buffCanvasGroup.alpha = 1.0f;
            }

            yield return null;
        }

        // 버프 종료 시 알파값 복구 및 비활성화
        buffCanvasGroup.alpha = 1.0f;
        buffCanvasGroup.gameObject.SetActive(false);
    }
}