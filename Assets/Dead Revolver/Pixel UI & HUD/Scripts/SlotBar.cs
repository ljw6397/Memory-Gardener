using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace PixelUI {
    public class SlotBar : MonoBehaviour {
        public delegate void SlotsIncreaseDelegate(int slots, int increaseAmount);

        public delegate void SlotsDecreaseDelegate(int slots, int decreaseAmount);

        public int MaxSlots = 5;
        public int MinSlots = 0;

        public int CurrentSlots {
            get => currentSlots;
            set => SetSlots(value);
        }
        [SerializeField]
        private int currentSlots;

        public GameObject SlotPrefab;
        public Transform SlotContainer;
        public int DisplaySlots { get; set; }
        public UnityEvent<int, int> OnSlotsIncreased;
        public UnityEvent<int, int> OnSlotsDecreased;
        public UnityEvent<int> OnSlotsChanged;
        public UnityEvent<int> OnDisplaySlotsChanged;
        public UnityEvent<int> OnFollowUpSlotsChanged;
        public UnityEvent<int> OnEmptied;
        public UnityEvent<int> OnFilled;

        private List<GameObject> spawnedSlots = new();

        // public float FollowUpDelay = 1;
        // public Timer FollowUpTimer;
        // private float followUpTweenDuration = 0.2f;
        // private EaseType followUpEaseType = EaseType.QuadOut;
        // private float tweenDuration = 0.1f;
        // private EaseType easeType = EaseType.QuadOut;

        public SlotBar(int maxSlots = 100, int currentSlots = 100) {
            MaxSlots = maxSlots;
            CurrentSlots = currentSlots;
            DisplaySlots = CurrentSlots;
            MinSlots = 0;
        }

        public void Start() {
            UpdateSlots();
        }

        /// <summary>
        /// Increases the current slots and returns the new current amount.
        /// </summary>
        public int Increase(int amount) {
            SetSlots(CurrentSlots + amount);
            return CurrentSlots;
        }

        /// <summary>
        /// Decreases the current slots and returns the new current amount.
        /// </summary>
        public int Decrease(int amount) {
            SetSlots(CurrentSlots - amount);
            return CurrentSlots;
        }

        /// <summary>
        /// Sets the current amount of slots.
        /// </summary>
        public void SetSlots(int value) {
            if (value == currentSlots) return;
            if (currentSlots <= MinSlots && value <= MinSlots) return;
            if (currentSlots >= MaxSlots && value >= MaxSlots) return;

            var difference = value - currentSlots;
            var differenceAbsolute = Math.Abs(difference);
            var isIncrease = difference > 0;

            if (value >= MaxSlots) {
                OnFilled?.Invoke(CurrentSlots);
            }

            if (value <= MinSlots) {
                OnEmptied?.Invoke(CurrentSlots);
            }

            var slots = GetSlots();

            if (isIncrease) {
                for (var i = currentSlots; i < value; i++) {
                    if (i < slots.Count) {
                        var slot = slots[i];
                        slot.Heal();
                    }
                }
            } else {
                for (var i = currentSlots; i > value; i--) {
                    if (i > 0) {
                        var slot = slots[i - 1];
                        slot.Damage();
                    }
                }
            }

            currentSlots = Mathf.Clamp(value, MinSlots, MaxSlots);
            OnSlotsChanged?.Invoke(currentSlots);

            if (isIncrease) {
                OnSlotsIncreased?.Invoke(CurrentSlots, differenceAbsolute);
            } else if (difference < 0) {
                OnSlotsDecreased?.Invoke(CurrentSlots, differenceAbsolute);
            }
        }

        public float GetPercentage() {
            return GetNormalized() * 100;
        }

        public float GetNormalized() {
            return CurrentSlots / MaxSlots;
        }

        // public void OnValidate() {
        //     if (SlotPrefab == null || SlotContainer == null) return;
        //
        //     UpdateSlots();
        // }

        public List<ValueSlot> GetSlots() {
            var slots = new List<ValueSlot>();

            foreach (Transform child in SlotContainer) {
                var slot = child.GetComponent<ValueSlot>();
                slots.Add(slot);
            }

            return slots;
        }

        public void UpdateSlots() {
            // foreach (var life in spawnedSlots) {
            //     if (life) {
            //         Destroy(life);
            //         DestroyImmediate(life);
            //     }
            // }
            // while (transform.childCount > 0) {
            //     DestroyImmediate(transform.GetChild(0).gameObject);
            // }
            // #if UNITY_EDITOR
            // OnValidateSafe();
            // EditorApplication.delayCall += () => {
            //     if (this != null) {
            //         OnValidateSafe();
            //         EditorUtility.SetDirty(this);
            //     }
            // };
            // #endif
            OnValidateSafe();
        }

        void OnValidateSafe() {
            // if (spawnedSlots is not null) {
            //     foreach (var spawned in spawnedSlots) {
            //         if (Application.isPlaying) {
            //             Destroy(spawned.gameObject);
            //         } else {
            //             DestroyImmediate(spawned.gameObject, true);
            //         }
            //     }
            // }
            //
            // foreach (Transform child in SlotContainer) {
            //     if (Application.isPlaying) {
            //         Destroy(child.gameObject);
            //     } else {
            //         DestroyImmediate(child.gameObject, true);
            //     }
            // }
            
            ClearSlots();
            currentSlots = Mathf.Clamp(CurrentSlots, MinSlots, MaxSlots);
            spawnedSlots?.Clear();

            for (var i = 0; i < MaxSlots; i++) {
                if (SlotContainer.gameObject.scene.IsValid()) {
                    var newLife = Instantiate(SlotPrefab);
                    var slot = newLife.GetComponent<ValueSlot>();

                    newLife.transform.SetParent(SlotContainer, false);
                    newLife.name = "Life " + (i + 1);
                    spawnedSlots?.Add(newLife);

                    if (i < CurrentSlots) {
                        slot.SetState(ValueSlot.ValueState.Full);
                    } else {
                        slot.SetState(ValueSlot.ValueState.Empty);
                    }
                }
            }
        }

        public void ClearSlots() {
            if (SlotContainer == null) return;

            while (SlotContainer.transform.childCount > 0) {
                DestroyImmediate(SlotContainer.transform.GetChild(0).gameObject);
            }
        }
    }
}