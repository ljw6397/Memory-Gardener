using UnityEngine;

namespace PixelUI {
    public class Popup : MonoBehaviour {
        private Animator animator;

        void Awake() {
            animator = GetComponent<Animator>();
        }

        public void Show() {
            animator.SetTrigger("Show");
        }

        public void Hide() {
            animator.SetTrigger("Hide");
        }
    }
}