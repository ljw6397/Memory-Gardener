using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelUI.Demo {
    public class ValueSlotsDemo : MonoBehaviour {
        void Start() { }

        void Update() { }

        public void Heal(int amount) {
            var slotBars = FindObjectsByType<SlotBar>(FindObjectsSortMode.None);

            foreach (var bar in slotBars) {
                bar.Increase(amount);
            }
        }
        
        public void Damage(int amount) {
            var slotBars = FindObjectsByType<SlotBar>(FindObjectsSortMode.None);

            foreach (var bar in slotBars) {
                bar.Decrease(amount);
            }
        }
    }
}