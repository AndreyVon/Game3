using UnityEngine;

public class TileMovement
{
    public TileView View;
    public Vector3 StartPosition;
    public Vector3 TargetPosition;

    public TileMovement(TileView view, Vector3 startPosition, Vector3 targetPosition)
    {
        View = view;
        StartPosition = startPosition;
        TargetPosition = targetPosition;
    }
}