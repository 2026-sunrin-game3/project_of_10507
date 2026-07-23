using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings")]
    public float defaultDuration = 0.2f;   // 흔들림 지속 시간
    public float defaultMagnitude = 0.3f;  // 흔들림 세기

    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 lastAppliedOffset = Vector3.zero; // 지난 프레임에 적용된 흔들림 값

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        float d = duration > 0 ? duration : defaultDuration;
        float m = magnitude > 0 ? magnitude : defaultMagnitude;

        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(d, m));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 흔들림 종료 시 오프셋 초기화
        shakeOffset = Vector3.zero;
    }

    private void LateUpdate()
    {
        // 1. 지난 프레임에 더해졌던 흔들림 오프셋을 빼서 원래 위치(0,0 기준)로 원상복구
        transform.position -= lastAppliedOffset;

        // 2. 이번 프레임의 새로운 흔들림 오프셋을 기록하고 적용
        lastAppliedOffset = shakeOffset;
        transform.position += lastAppliedOffset;
    }
}