using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PixelUI {
    public class Grid : MonoBehaviour {
        public Selector Selector;
        public InventoryTooltip Tooltip;

        private GridSlot[] slots;
        private ListItem activeItem;

        public void Start() {
            slots = GetComponentsInChildren<GridSlot>();
            Tooltip.Hide();
            
            foreach (var slot in slots) {
                var button = slot.GetComponent<UnityEngine.UI.Button>();

                button.onClick.AddListener(() => {
                    Selector.SetTarget(slot.transform);
                });

                var pixelButton = slot.GetComponent<Button>();

                if (pixelButton) {
                    pixelButton.OnPointerExited.AddListener(() => {
                        Tooltip.Hide();
                    });
                    pixelButton.OnPointerEntered.AddListener(() => {
                        var item = slot.GetComponent<InventoryItem>();

                        if (!item || item.Name == "") {
                            Tooltip.Hide();
                            return;
                        }
                        
                        Tooltip.Show();

                        if (item) {
                            Tooltip.SetItem(item);
                        }
                    });
                }
            }
        }
    }
}