using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    public Color outlineColor = Color.red;
    public float outlineThickness = 0.03f; // 월드 유닛 기준 두께
    public int outlineDirections = 8;      // 4면 8방향 촘촘하게

    private SpriteRenderer mainRenderer;
    private SpriteRenderer[] outlineRenderers;
    private Transform outlineParent;

    void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();

        // 아웃라인 전용 자식 오브젝트 하나 만들어서 그 밑에 복제본들 정리
        GameObject parentObj = new GameObject("OutlineSprites");
        parentObj.transform.SetParent(transform, false);
        outlineParent = parentObj.transform;

        outlineRenderers = new SpriteRenderer[outlineDirections];

        for (int i = 0; i < outlineDirections; i++)
        {
            GameObject copy = new GameObject("Outline_" + i);
            copy.transform.SetParent(outlineParent, false);

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.color = outlineColor;
            outlineRenderers[i] = sr;
        }
    }

    void LateUpdate()
    {
        // 매 프레임 본체와 완전히 동일한 스프라이트/방향으로 맞춤 (애니메이션 자동 추적)
        for (int i = 0; i < outlineDirections; i++)
        {
            SpriteRenderer sr = outlineRenderers[i];

            sr.sprite = mainRenderer.sprite;
            sr.flipX = mainRenderer.flipX;
            sr.flipY = mainRenderer.flipY;
            sr.sortingLayerID = mainRenderer.sortingLayerID;
            sr.sortingOrder = mainRenderer.sortingOrder - 1; // 본체보다 한 칸 뒤에 그려지게

            float angle = (360f / outlineDirections) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * outlineThickness;
            outlineRenderers[i].transform.localPosition = offset;
        }
    }
}
