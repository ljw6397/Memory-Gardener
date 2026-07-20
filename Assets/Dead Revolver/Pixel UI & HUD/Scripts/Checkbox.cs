using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelUI {
    public class Checkbox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        public Image Image;
        public Sprite CheckedSprite;
        public Sprite UncheckedSprite;
        public Sprite HoverSprite;

        private Toggle toggle;
        private bool isMouseOver;

        public void Awake() {
            toggle = GetComponent<Toggle>();

            if (toggle.isOn) {
                OnValueChanged(true);
            }
        }

        public void OnValueChanged(bool value) {
            if (Image is null) return;

            if (!isMouseOver || !HoverSprite) {
                Image.sprite = value ? CheckedSprite : UncheckedSprite;
            } else {
                Image.sprite = value ? CheckedSprite : HoverSprite;
            }

            Image.SetNativeSize();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            isMouseOver = true;

            if (Image is null || !HoverSprite) return;

            Image.sprite = toggle.isOn ? CheckedSprite : HoverSprite;
            Image.SetNativeSize();
        }

        public void OnPointerExit(PointerEventData eventData) {
            isMouseOver = false;

            if (Image is null) return;

            Image.sprite = toggle.isOn ? CheckedSprite : UncheckedSprite;
            Image.SetNativeSize();
        }

        public void OnPointerClick(PointerEventData eventData) {
            toggle.isOn = !toggle.isOn;
        }

        public void OnValidate() {
            OnValueChanged(GetComponent<Toggle>().isOn);
        }
    }
}