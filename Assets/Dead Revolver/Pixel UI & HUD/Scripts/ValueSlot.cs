using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class ValueSlot : MonoBehaviour {
        public enum ValueState {
            Full,
            Damaged,
            Empty
        }

        public Sprite Full;
        public Sprite Damaged;
        public Sprite Empty;
        public Image Image;
        public ValueState State;

        private Animator animator;

        public void Awake() {
            animator = GetComponent<Animator>();
        }

        public void Heal() {
            animator?.SetTrigger("Heal");
        }

        public void Damage() {
            animator?.SetTrigger("Damage");
        }

        public void SetState(ValueState state) {
            State = state;
            UpdateUI();
        }

        private void UpdateUI() {
            if (Image is null || Image.gameObject is null) return;

            switch (State) {
                case ValueState.Full:
                    Image.sprite = Full;
                    break;
                case ValueState.Damaged:
                    Image.sprite = Damaged;
                    break;
                case ValueState.Empty:
                    Image.sprite = Empty;
                    break;
            }
        }

        public void OnValidate() {
            UpdateUI();
        }
    }
}