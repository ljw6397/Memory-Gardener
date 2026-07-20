using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class SkillTreeTooltip : MonoBehaviour {
        public SkillItem Skill;
        public TextMeshProUGUI TitleLabel;
        public TextMeshProUGUI DescriptionLabel;
        public TextMeshProUGUI CategoryLabel;
        public Vector2 Offset;

        private Animator animator;

        void Awake() {
            animator = GetComponent<Animator>();
        }

        void Start() { }

        void Update() { }

        public void SetSkill(SkillItem skill) {
            Skill = skill;
            TitleLabel.text = Skill.Name;
            DescriptionLabel.text = Skill.Description;
            CategoryLabel.text = Skill.Category;
            transform.position = skill.transform.position;
        }

        public void Show() {
            if (animator) {
                animator.SetTrigger("Show");
            } else {
                gameObject.SetActive(true);
            }
        }

        public void Hide() {
            if (animator) {
                animator.SetTrigger("Hide");
            } else {
                gameObject.SetActive(false);
            }
        }
    }
}