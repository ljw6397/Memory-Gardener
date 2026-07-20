using UnityEngine;

namespace PixelUI {
    public class SkillLine : MonoBehaviour {
        private Animator animator;

        void Start() {
            animator = GetComponent<Animator>();
        }

        public void Select() {
            animator.SetTrigger("Select");
        }
    }
}