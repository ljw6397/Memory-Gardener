using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class GridSlot : MonoBehaviour {
        public enum SlotState {
            Active,
            Inactive,
        }

        public SlotState State;
        public Sprite Active;
        public Sprite Inactive;
        public Image Image;

        public void SetState(SlotState state) {
            State = state;
            UpdateUI();
        }

        private void UpdateUI() {
            if (Image?.gameObject is null) return;

            switch (State) {
                case SlotState.Active:
                    Image.sprite = Active;
                    break;
                case SlotState.Inactive:
                    Image.sprite = Inactive;
                    break;
            }
        }

        public void OnValidate() {
            UpdateUI();
        }
    }
}