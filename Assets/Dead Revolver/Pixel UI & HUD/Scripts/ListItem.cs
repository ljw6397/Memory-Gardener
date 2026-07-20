using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelUI {
    public class ListItem : MonoBehaviour {
        public enum ListItemState {
            Normal,
            Selected,
        }

        public ListItemState State;
        public Image Image;
        public TextMeshProUGUI TextLabel;
        public Sprite Normal;
        public Sprite Selected;
        public string Text;
        public Color NormalTextColor;
        public Color SelectedTextColor;
        public UnityEvent OnClick;

        private UnityEngine.UI.Button button;
        private Animator animator;

        private void Awake() {
            button = GetComponent<UnityEngine.UI.Button>();
            animator = GetComponent<Animator>();
            button.onClick.AddListener(HandleClick);
            SetText(Text);
        }

        private void HandleClick() {
            OnClick?.Invoke();
        }

        public void SetState(ListItemState state) {
            State = state;
            UpdateUI();
        }

        public void SetText(string text) {
            Text = text;
            TextLabel.text = Text;
        }

        private void UpdateUI() {
            if (Image?.gameObject is null) return;

            switch (State) {
                case ListItemState.Normal:
                    TextLabel.color = NormalTextColor;
                    Image.sprite = Normal;

                    if (animator) {
                        animator.SetTrigger("Normal");
                    }

                    break;
                case ListItemState.Selected:
                    TextLabel.color = SelectedTextColor;
                    Image.sprite = Selected;

                    if (animator) {
                        animator?.SetTrigger("Selected");
                    }

                    break;
            }
        }

        public void OnValidate() {
            UpdateUI();
        }
    }
}