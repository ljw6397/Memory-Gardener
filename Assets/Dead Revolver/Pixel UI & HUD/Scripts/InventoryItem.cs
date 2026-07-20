using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class InventoryItem : MonoBehaviour {
        public string Name;
        public string Description;
        public Sprite Icon;
        public int Count = 1;

        [Header("UI")]
        public Image IconImage;
        public TextMeshProUGUI CountText;

        void Awake() {
            UpdateUI();
        }

        void OnValidate() {
            UpdateUI();
        }

        private void UpdateUI() {
            if (IconImage) {
                if (Icon) {
                    IconImage.sprite = Icon;
                    IconImage.SetNativeSize();
                    IconImage.gameObject.SetActive(true);
                    CountText.gameObject.SetActive(true);
                } else {
                    IconImage.gameObject.SetActive(false);
                    CountText.gameObject.SetActive(false);
                }
            }

            if (CountText) {
                CountText.text = Count.ToString();
            }
        }
    }
}