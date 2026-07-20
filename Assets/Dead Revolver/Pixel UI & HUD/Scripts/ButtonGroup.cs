using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace PixelUI {
    public class ButtonGroup : MonoBehaviour {
        public TextMeshProUGUI Label;
        public Transform ButtonContainer;
        public UnityEngine.UI.Button SelectedButton;

        private UnityEngine.UI.Button[] buttons;

        public void Start() {
            buttons = ButtonContainer.GetComponentsInChildren<UnityEngine.UI.Button>();

            foreach (var button in buttons) {
                button.onClick.AddListener(() => {
                    var label = button.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (label && Label) {
                        Label.text = label.text;
                    }

                    if (SelectedButton is not null) {
                        SelectedButton.GetComponent<Animator>().SetBool("IsSelected", false);
                    }

                    SelectedButton = button;
                    SelectedButton.GetComponent<Animator>().SetBool("IsSelected", true);
                });
            }

            if (SelectedButton is not null) {
                SelectButton(SelectedButton);
            }
        }

        public void OnButtonClicked() { }

        public void Next() {
            var index = SelectedButton.transform.GetSiblingIndex();
            index++;

            if (index > buttons.Length - 1) {
                index = 0;
            }

            foreach (var button in buttons) {
                if (button.transform.GetSiblingIndex() == index) {
                    SelectButton(button);
                }
            }
        }

        private void SelectButton(UnityEngine.UI.Button button) {
            button.onClick?.Invoke();
            SelectedButton = button;
        }

        public void Previous() {
            var index = SelectedButton.transform.GetSiblingIndex();
            index--;

            if (index < 0) {
                index = buttons.Length - 1;
            }

            foreach (var button in buttons) {
                if (button.transform.GetSiblingIndex() == index) {
                    button.onClick?.Invoke();
                    SelectedButton = button;
                }
            }
        }
    }
}