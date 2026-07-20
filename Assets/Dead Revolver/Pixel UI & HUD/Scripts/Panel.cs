using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class Panel : MonoBehaviour {
        public enum PanelState {
            Active,
            Inactive,
        }

        public PanelState State;
        public Sprite Active;
        public Sprite Inactive;
        public Image Image;

        public void SetState(PanelState state) {
            State = state;
            UpdateUI();
        }

        private void UpdateUI() {
            if (Image is null || Image.gameObject is null) return;

            switch (State) {
                case PanelState.Active:
                    Image.sprite = Active;
                    break;
                case PanelState.Inactive:
                    Image.sprite = Inactive;
                    break;
            }
        }

        public void OnValidate() {
            UpdateUI();
        }
    }
}