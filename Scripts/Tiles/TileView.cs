using UnityEngine;

public class TileView : MonoBehaviour
{
    private Tile tile;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer highlightRenderer;
    private TrailRenderer speedTrail;

    private Vector3 defaultScale;
    private Color defaultColor;

    public Tile Tile => tile;

    public void Initialize(Tile newTile)
    {
        tile = newTile;

        spriteRenderer = GetComponent<SpriteRenderer>();

        defaultScale = transform.localScale;

        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;

            CreateHighlightRendererIfNeeded();
            CreateSpeedTrailIfNeeded();
        }
    }

    public void Select()
    {
        transform.localScale = defaultScale * 1.12f;

        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = true;
        }
    }

    public void Deselect()
    {
        transform.localScale = defaultScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = false;
        }
    }

    public void SetSpeedTrailActive(bool isActive)
    {
        CreateSpeedTrailIfNeeded();

        if (speedTrail == null)
        {
            return;
        }

        if (isActive)
        {
            speedTrail.Clear();
            speedTrail.emitting = true;
        }
        else
        {
            speedTrail.emitting = false;
        }
    }

    public void RefreshName()
    {
        if (tile == null)
        {
            gameObject.name = "TileView_NULL";
            return;
        }

        gameObject.name = $"TileView ({tile.X}, {tile.Y}) Type {tile.Type}";
    }

    private void CreateHighlightRendererIfNeeded()
    {
        if (highlightRenderer != null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            return;
        }

        GameObject highlightObject = new GameObject("White Highlight");

        highlightObject.transform.SetParent(transform);
        highlightObject.transform.localPosition = Vector3.zero;
        highlightObject.transform.localRotation = Quaternion.identity;
        highlightObject.transform.localScale = Vector3.one * 1.03f;

        highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();

        highlightRenderer.sprite = spriteRenderer.sprite;
        highlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        highlightRenderer.color = new Color(1f, 1f, 1f, 0.22f);
        highlightRenderer.enabled = false;
    }

    private void CreateSpeedTrailIfNeeded()
    {
        if (speedTrail != null)
        {
            return;
        }

        speedTrail = GetComponent<TrailRenderer>();

        if (speedTrail == null)
        {
            speedTrail = gameObject.AddComponent<TrailRenderer>();
        }

        speedTrail.time = 0.12f;
        speedTrail.startWidth = 0.45f;
        speedTrail.endWidth = 0f;
        speedTrail.minVertexDistance = 0.02f;

        speedTrail.autodestruct = false;
        speedTrail.emitting = false;

        speedTrail.sortingLayerID = spriteRenderer.sortingLayerID;
        speedTrail.sortingOrder = spriteRenderer.sortingOrder - 1;

        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        speedTrail.material = trailMaterial;

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.35f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        speedTrail.colorGradient = gradient;
    }
}