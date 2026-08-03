using UnityEngine;
using UnityEngine.UI;

public class PlayerHeartUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Image[] heartImages = new Image[3]; 

    [Header("Sprites (하트 하나당 필요한 3가지 상태)")]
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite;
    public Sprite emptyHeartSprite;

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHearts;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHearts;
    }

    void Start()
    {
        if (playerHealth != null)
            UpdateHearts(playerHealth.CurrentSegments, playerHealth.MaxSegments);
    }

    void UpdateHearts(int currentSegments, int maxSegments)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            int segmentsInThisHeart = Mathf.Clamp(currentSegments - (i * 2), 0, 2);

            if (segmentsInThisHeart >= 2)
                heartImages[i].sprite = fullHeartSprite;
            else if (segmentsInThisHeart == 1)
                heartImages[i].sprite = halfHeartSprite;
            else
                heartImages[i].sprite = emptyHeartSprite;
        }
    }
}