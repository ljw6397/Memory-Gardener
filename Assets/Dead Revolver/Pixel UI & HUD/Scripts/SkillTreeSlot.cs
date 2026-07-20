using UnityEngine;
using UnityEngine.Events;

namespace PixelUI {
    public class SkillTreeSlot : MonoBehaviour {
        public UnityEvent OnSelected;

        private Animator animator;

        void Start() {
            animator = GetComponent<Animator>();
        }

        public void Select() {
            animator.SetTrigger("Select");
            OnSelected?.Invoke();
        }
    }
}