using System.Collections;
using UnityEngine;

public class KnifeEffect : MonoBehaviour
{
    [Header("Knife Parts")]
    public Transform knifePivot;

    [Header("Chop Animation")]
    public float startAngle = 18f;
    public float chopAngle = -35f;
    public float chopTime = 0.08f;
    public float pauseBetweenChops = 0.03f;
    public int chopCount = 2;

    private void Awake()
    {
        if (knifePivot == null)
        {
            knifePivot = transform;
        }
    }

    public IEnumerator Play()
    {
        if (knifePivot == null)
        {
            Destroy(gameObject);
            yield break;
        }

        SetPivotAngle(startAngle);

        for (int i = 0; i < chopCount; i++)
        {
            yield return RotatePivotRoutine(startAngle, chopAngle, chopTime);
            yield return RotatePivotRoutine(chopAngle, startAngle, chopTime);

            if (pauseBetweenChops > 0f)
            {
                yield return new WaitForSeconds(pauseBetweenChops);
            }
        }

        Destroy(gameObject);
    }

    private IEnumerator RotatePivotRoutine(float fromAngle, float toAngle, float duration)
    {
        if (duration <= 0f)
        {
            SetPivotAngle(toAngle);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentAngle = Mathf.Lerp(fromAngle, toAngle, smoothT);

            SetPivotAngle(currentAngle);

            yield return null;
        }

        SetPivotAngle(toAngle);
    }

    private void SetPivotAngle(float angle)
    {
        knifePivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}