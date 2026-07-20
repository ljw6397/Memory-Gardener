using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class SkillItem : MonoBehaviour {
        public string Name;
        public string Description;
        public string Category;
        public Sprite Icon;

        [Header("UI")]
        public Image IconImage;

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
                } else {
                    IconImage.gameObject.SetActive(false);
                }
            }
        }
    }
}