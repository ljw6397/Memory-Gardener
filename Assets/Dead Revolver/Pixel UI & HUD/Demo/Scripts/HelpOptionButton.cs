using System.Reflection.Emit;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace PixelUI.Demo {
    public class HelpOptionButton : MonoBehaviour {
        public string SceneName;
        public TextMeshProUGUI Label;

        void Awake() {
            if (SceneManager.GetActiveScene().name == SceneName) {
                Color myColor;
                ColorUtility.TryParseHtmlString("#00DCFF", out myColor);
                Label.color = myColor;
            }
        }

        public void GoToScene() {
            SceneManager.LoadScene(SceneName);
        }

        void Start() {

        }

        void Update() {

        }
    }
}