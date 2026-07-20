using UnityEngine;
using UnityEngine.Events;

namespace PixelUI {
    public class Selector : MonoBehaviour {
        public UnityEvent OnSelected;
        public Transform Target;
        public float FollowSpeed = 100;

        private Animator animator;

        void Start() {
            animator = GetComponent<Animator>();
        }

        public void Select() {
            animator.SetTrigger("Select");
            OnSelected?.Invoke();
        }

        void Update() {
            if (Target && transform.position != Target.position) {
                transform.position = Vector3.MoveTowards(transform.position, Target.position, Time.deltaTime * FollowSpeed);
            }
        }

        public void SetTarget(Transform target) {
            Target = target;
        }
    }
}