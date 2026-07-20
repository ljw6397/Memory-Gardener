using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelUI {
    public class ValueBar : MonoBehaviour {
        public enum ValueBarMode {
            Horizontal,
            Vertical
        }

        public delegate void ValueDecreaseDelegate(float value, float decreaseAmount);

        public delegate void ValueIncreaseDelegate(float value, float increaseAmount);

        public float MaxValue = 100f;
        public float MinValue = 0;

        public float CurrentValue {
            get => currentValue;
            set => SetValue(value);
        }
        [SerializeField]
        private float currentValue;

        public float DisplayValue { get; set; }
        public float FollowUpValue { get; set; }
        public float AnimationSpeed = 2f;
        public Action<float> OnValueChanged;
        public Action<float> OnDisplayValueChanged;
        public Action<float> OnFollowUpValueChanged;
        public Action<float> OnEmptied;
        public Action<float> OnFilled;
        public event ValueIncreaseDelegate OnValueIncreased;
        public event ValueDecreaseDelegate OnValueDecreased;
        public Image FillImage;
        public Image FollowUpImage;
        public float FillPadding = 0.0f;
        public ValueBarMode Mode = ValueBarMode.Horizontal;

        void Start() {
            DisplayValue = CurrentValue;
            UpdateUI();
        }

        /// <summary>
        /// Increases the current value and returns the new current amount.
        /// </summary>
        public float Increase(float amount) {
            SetValue(CurrentValue + amount);
            return CurrentValue;
        }

        /// <summary>
        /// Decreases the current value and returns the new current amount.
        /// </summary>
        public float Decrease(float amount) {
            SetValue(CurrentValue - amount);
            return CurrentValue;
        }

        /// <summary>
        /// Sets the current value.
        /// </summary>
        public void SetValue(float value) {
            if (value == currentValue) return;
            if (currentValue <= MinValue && value <= MinValue) return;
            if (currentValue >= MaxValue && value >= MaxValue) return;

            var difference = value - currentValue;
            var differenceAbsolute = Math.Abs(difference);
            var isIncrease = difference > 0;

            if (value >= MaxValue) {
                OnFilled?.Invoke(CurrentValue);
            }

            if (value <= MinValue) {
                OnEmptied?.Invoke(CurrentValue);
            }

            currentValue = Mathf.Clamp(value, MinValue, MaxValue);
            OnValueChanged?.Invoke(currentValue);
            UpdateUI();

            if (isIncrease) {
                OnValueIncreased?.Invoke(CurrentValue, differenceAbsolute);
            } else if (difference < 0) {
                OnValueDecreased?.Invoke(CurrentValue, differenceAbsolute);
            }
        }

        public float GetPercentage() {
            return GetNormalized() * 100;
        }

        public float GetNormalized() {
            return CurrentValue / MaxValue;
        }

        public void OnValidate() {
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                DisplayValue = currentValue;
            }
            #endif

            UpdateUI();
        }

        public void UpdateUI() {
            UpdateFill();
            UpdateFollowUp();
        }

        public void UpdateFill() {
            if (FillImage == null) return;

            var parentRect = FillImage.transform.parent.GetComponent<RectTransform>();

            if (parentRect == null) return;

            var parentWidth = parentRect.rect.width;
            var healthPercentage = Mathf.Clamp(DisplayValue / MaxValue, 0, 1);
            var newWidth = parentWidth * healthPercentage;

            switch (Mode) {
                case ValueBarMode.Horizontal:
                    var rectTransform = FillImage.rectTransform;
                    rectTransform.sizeDelta = new Vector2(newWidth - FillPadding, rectTransform.sizeDelta.y);
                    break;
                case ValueBarMode.Vertical:
                    FillImage.fillAmount = healthPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateFollowUp() {
            if (FollowUpImage == null) return;

            var parentRect = FollowUpImage.transform.parent.GetComponent<RectTransform>();

            if (parentRect == null) return;

            var parentWidth = parentRect.rect.width;
            var healthPercentage = Mathf.Clamp(FollowUpValue / MaxValue, 0, 1);
            var newWidth = parentWidth * healthPercentage;

            switch (Mode) {
                case ValueBarMode.Horizontal:
                    var rectTransform = FollowUpImage.rectTransform;
                    rectTransform.sizeDelta = new Vector2(newWidth - FillPadding, rectTransform.sizeDelta.y);
                    break;
                case ValueBarMode.Vertical:
                    FollowUpImage.fillAmount = healthPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Update() {
            if (DisplayValue != CurrentValue) {
                DisplayValue = Mathf.Lerp(DisplayValue, CurrentValue, AnimationSpeed * Time.deltaTime);
                UpdateUI();
            }
        }
    }
}