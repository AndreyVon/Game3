using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitBackgroundToCamera : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void Fit()
    {
        if (mainCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        transform.position = new Vector3(
            mainCamera.transform.position.x,
            mainCamera.transform.position.y,
            transform.position.z
        );

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float scaleX = cameraWidth / spriteSize.x;
        float scaleY = cameraHeight / spriteSize.y;

        float finalScale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(finalScale, finalScale, 1f);
    }
}