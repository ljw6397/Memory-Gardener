using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class InventoryTooltip : MonoBehaviour {
        public InventoryItem Item;
        public TextMeshProUGUI TitleLabel;
        public TextMeshProUGUI DescriptionLabel;
        public Vector2 Offset;

        private Animator animator;

        void Awake() {
            animator = GetComponent<Animator>();
        }

        void Start() { }

        void Update() { }

        public void SetItem(InventoryItem item) {
            Item = item;
            TitleLabel.text = Item.Name;
            DescriptionLabel.text = Item.Description;
            transform.position = item.transform.position;
        }

        public void Show() {
            if (animator) {
                animator.SetTrigger("Show");
            } else {
                gameObject.SetActive(true);
            }
        }

        public void Hide() {
            if (animator) {
                animator.SetTrigger("Hide");
            } else {
                gameObject.SetActive(false);
            }
        }
    }
}