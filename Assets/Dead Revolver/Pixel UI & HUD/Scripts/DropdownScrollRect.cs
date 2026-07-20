using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class DropdownScrollRect : MonoBehaviour {
        private ScrollRect scrollRect;

        public void Awake() {
            scrollRect = GetComponent<ScrollRect>();
        }

        public void LateUpdate() {
            if (scrollRect.verticalScrollbar) {
                scrollRect.verticalScrollbar.size = 0;
            }
        }
    }
}