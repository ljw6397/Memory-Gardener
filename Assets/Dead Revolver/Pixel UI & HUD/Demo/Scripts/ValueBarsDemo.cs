using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelUI.Demo {
    public class ValueBarsDemo : MonoBehaviour {
        void Start() { }

        void Update() { }

        public void Heal(float amount) {
            var valueBars = GameObject.FindObjectsByType<ValueBar>(FindObjectsSortMode.None);

            foreach (var bar in valueBars) {
                bar.Increase(amount);
            }
        }
        
        public void Damage(float amount) {
            var valueBars = GameObject.FindObjectsByType<ValueBar>(FindObjectsSortMode.None);

            foreach (var bar in valueBars) {
                bar.Decrease(amount);
            }
        }
    }
}