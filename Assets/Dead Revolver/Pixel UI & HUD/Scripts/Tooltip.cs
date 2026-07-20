using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    [ExecuteInEditMode]
    public class Tooltip : MonoBehaviour {
        public enum TooltipType {
            TopLeft,
            TopRight,
            RightTop,
            RightBottom,
            BottomLeft,
            BottomRight,
            LeftTop,
            LeftBottom
        }

        [System.Serializable]
        public class ImageMapping {
            public TooltipType type;
            public Sprite sprite;
        }

        public TooltipType Type;
        public Image targetImage;
        public List<ImageMapping> imageMappings;

        private Dictionary<TooltipType, Sprite> imageDictionary;

        void Awake() {
            UpdateImage();
        }

        private void UpdateImage() {
            imageDictionary = new Dictionary<TooltipType, Sprite>();

            foreach (var mapping in imageMappings) {
                if (!imageDictionary.ContainsKey(mapping.type)) {
                    imageDictionary.Add(mapping.type, mapping.sprite);
                }
            }

            SetImage(Type);
        }

        public void SetImage(TooltipType type) {
            if (imageDictionary.TryGetValue(type, out var sprite)) {
                targetImage.sprite = sprite;
            }
        }

        public void OnValidate() {
            UpdateImage();
        }
    }
}