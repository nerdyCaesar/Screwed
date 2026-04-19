using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;
    private Vector3 _originalPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        _originalPos = Camera.main.transform.localPosition;
    }

    public void Shake(float duration = 0.3f, float magnitude = 0.15f)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.localPosition = new Vector3(
                _originalPos.x + x,
                _originalPos.y + y,
                _originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = _originalPos;
    }
}