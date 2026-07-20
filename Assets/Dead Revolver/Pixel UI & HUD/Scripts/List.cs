using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelUI {
    public class List : MonoBehaviour {
        [System.Serializable]
        public class ListItemSettings {
            public string Text;
            public bool IsSelected;
            public UnityEvent OnSelected;
        }

        public RectTransform ListContainer;
        public GameObject ListItemPrefab;
        public List<ListItemSettings> ListItems = new();

        private List<ListItem> listItems = new();
        private ListItem activeItem;

        public void Awake() {
            UpdateItems();
        }

        public void AddItem(ListItemSettings itemSettings) {
            var newItem = Instantiate(ListItemPrefab, ListContainer);
            newItem.name = itemSettings.Text;
            var item = newItem.GetComponent<ListItem>();
            item.Text = itemSettings.Text;
            item.SetText(itemSettings.Text);
            item.OnClick.AddListener(() => SetActiveItem(item, itemSettings));
            listItems.Add(item);

            if (itemSettings.IsSelected) {
                SetActiveItem(item, itemSettings);
            }
        }

        public void SetActiveItem(ListItem selectedItem, ListItemSettings itemSettings) {
            if (activeItem != null) {
                activeItem.SetState(ListItem.ListItemState.Normal);
            }

            activeItem = selectedItem;
            activeItem.SetState(ListItem.ListItemState.Selected);
            itemSettings.OnSelected?.Invoke();
        }

        public void UpdateItems() {
            ClearList();

            foreach (var listItem in ListItems) {
                AddItem(listItem);
            }
        }

        public void ClearList() {
            if (ListContainer == null) return;

            while (ListContainer.transform.childCount > 0) {
                DestroyImmediate(ListContainer.transform.GetChild(0).gameObject);
            }
        }
    }
}