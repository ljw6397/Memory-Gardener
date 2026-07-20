using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelUI.Demo {
    public class DemoCoverTitle : MonoBehaviour {
        private List<ValueSlot> heartSlots = new List<ValueSlot>();
        private List<Animator> heartSlotAnimators = new List<Animator>();

        public void Start() {
            heartSlots = GetComponentsInChildren<ValueSlot>().ToList();

            foreach (var heartSlot in heartSlots) {
                var animator = heartSlot.GetComponent<Animator>();
                heartSlot.SetState(ValueSlot.ValueState.Empty);
                heartSlotAnimators.Add(animator);
            }
        }

        public void PlayHeartFillAnimation(int index) {
            heartSlotAnimators[index].SetTrigger("Heal");
        }

        public void PlayHeartBounceAnimation(int index) {
            heartSlotAnimators[index].SetTrigger("Bounce");
        }

        public void ResetHearts() {
            foreach (var heartSlot in heartSlots) {
                heartSlot.SetState(ValueSlot.ValueState.Empty);
            }
        }

        void Update() {
            if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1)) {
                var demoManager = GameObject.FindAnyObjectByType<DemoManager>();

                if (demoManager && !demoManager.IsOverlayOpen) {
                    demoManager.ShowOverlay();
                }
            }
        }
    }
}