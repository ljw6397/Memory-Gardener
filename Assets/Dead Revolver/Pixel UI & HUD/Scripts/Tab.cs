using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class Tab : MonoBehaviour {
        [System.Serializable]
        public class TabSprite {
            public TabPosition Position;
            public Sprite Active;
            public Sprite Inactive;
        }

        public enum TabState {
            Active,
            Inactive,
        }

        public enum TabPosition {
            TopLeft,
            Top,
            TopRight,
            RightTop,
            Right,
            RightBottom,
            BottomLeft,
            Bottom,
            BottomRight,
            LeftTop,
            Left,
            LeftBottom
        }

        public TabState State;
        public TabPosition Position;
        public Image Image;
        public List<TabSprite> Sprites;

        public void SetState(TabState state) {
            State = state;
            UpdateUI();
        }

        private void UpdateUI() {
            if (Image?.gameObject is null) return;

            var sprite = Sprites.First(s => s.Position == Position);

            switch (State) {
                case TabState.Active:
                    Image.sprite = sprite.Active;
                    break;
                case TabState.Inactive:
                    Image.sprite = sprite.Inactive;
                    break;
            }

            // Image.SetNativeSize();
        }

        public void OnValidate() {
            UpdateUI();
        }
    }
}