using UnityEngine;

namespace PixelUI {
    public class SkillTree : MonoBehaviour {
        public SkillTreeTooltip Tooltip;

        private SkillTreeSlot[] slots;
        private ListItem activeItem;

        public void Start() {
            slots = GetComponentsInChildren<SkillTreeSlot>();
            Tooltip.Hide();

            foreach (var slot in slots) {
                var pixelButton = slot.GetComponent<Button>();

                if (pixelButton) {
                    pixelButton.OnPointerExited.AddListener(() => {
                        Tooltip.Hide();
                    });
                    pixelButton.OnPointerEntered.AddListener(() => {
                        var item = slot.GetComponent<SkillItem>();

                        if (!item || item.Name == "") {
                            Tooltip.Hide();
                            return;
                        }

                        Tooltip.Show();

                        if (item) {
                            Tooltip.SetSkill(item);
                        }
                    });
                }
            }
        }
    }
}