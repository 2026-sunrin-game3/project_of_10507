using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 🔥 보스 처치 시 화면 전체 페이드아웃 → 대기 → 결과 이미지 표시
public class BossDeathEffect : MonoBehaviour
{
    [Header("Screen Fade")]
    [SerializeField] Image fadeImage;       // 화면 전체를 덮는 검은색 UI 이미지 (평소 alpha 0)
    [SerializeField] float fadeOutDuration = 1.5f;

    [Header("Result")]
    [SerializeField] GameObject resultImageObject; // 평소 비활성화
    [SerializeField] float delayBeforeImage = 1f;

    public GameObject Boss;


    public void PlayDeathSequence()
    {
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        Boss.SetActive(false); // 보스 오브젝트 비활성화
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            float t = 0f;

            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, t / fadeOutDuration);
                fadeImage.color = new Color(c.r, c.g, c.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(c.r, c.g, c.b, 1f);
        }

        yield return new WaitForSeconds(delayBeforeImage);

        if (resultImageObject != null)
        {
            resultImageObject.SetActive(true);
            fadeImage.gameObject.SetActive(false); // 결과 이미지 표시 후 페이드 이미지는 숨김
        }
    }
}