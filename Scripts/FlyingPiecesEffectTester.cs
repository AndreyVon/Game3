using UnityEngine;

public class FlyingPiecesEffectTester : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private FlyingPiecesEffect flyingPiecesEffect;

    [Header("Test Sprites")]
    [SerializeField] private Sprite[] testPieceSprites;

    [Header("Test Start Point")]
    [SerializeField] private Transform testWorldStartPoint;

    [Header("Input")]
    [SerializeField] private KeyCode testKey = KeyCode.Space;

    private void Start()
    {
        Debug.Log("FlyingPiecesEffectTester: Start сработал. “естер активен.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            Debug.Log("FlyingPiecesEffectTester: нажата клавиша " + testKey);
            PlayTest();
        }
    }

    private void PlayTest()
    {
        Debug.Log("FlyingPiecesEffectTester: PlayTest вызван.");

        if (flyingPiecesEffect == null)
        {
            Debug.LogWarning("FlyingPiecesEffectTester: FlyingPiecesEffect не назначен.");
            return;
        }

        if (testPieceSprites == null)
        {
            Debug.LogWarning("FlyingPiecesEffectTester: testPieceSprites == null.");
            return;
        }

        Debug.Log("FlyingPiecesEffectTester: количество спрайтов = " + testPieceSprites.Length);

        if (testPieceSprites.Length == 0)
        {
            Debug.LogWarning("FlyingPiecesEffectTester: Test Piece Sprites пустой. Size = 0.");
            return;
        }

        for (int i = 0; i < testPieceSprites.Length; i++)
        {
            if (testPieceSprites[i] == null)
            {
                Debug.LogWarning("FlyingPiecesEffectTester: Element " + i + " пустой.");
            }
            else
            {
                Debug.Log("FlyingPiecesEffectTester: Element " + i + " = " + testPieceSprites[i].name);
            }
        }

        if (testWorldStartPoint == null)
        {
            Debug.LogWarning("FlyingPiecesEffectTester: Test World Start Point не назначен.");
            return;
        }

        Debug.Log("FlyingPiecesEffectTester: стартова€ позици€ = " + testWorldStartPoint.position);

        flyingPiecesEffect.Play(testPieceSprites, testWorldStartPoint.position);

        Debug.Log("FlyingPiecesEffectTester: flyingPiecesEffect.Play вызван.");
    }
}