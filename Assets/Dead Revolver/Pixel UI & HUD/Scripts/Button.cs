using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PixelUI {
    public class Button : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerExitHandler {
        public UnityEvent OnSelected;
        public UnityEvent OnPointerEntered;
        public UnityEvent OnPointerExited;

        public void OnSelect(BaseEventData eventData) {
            OnSelected?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            OnPointerEntered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData) {
            OnPointerExited?.Invoke();
        }
    }
}