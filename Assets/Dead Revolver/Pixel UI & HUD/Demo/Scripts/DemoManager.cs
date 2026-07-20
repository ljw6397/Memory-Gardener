using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelUI.Demo {
    public class DemoManager : MonoBehaviour {
        public int ResolutionWidth = 400 * 4;
        public int ResolutionHeight = 300 * 4;
        public float CanvasScale = 1;
        public List<string> Scenes;
        [HideInInspector] public bool IsOverlayOpen;

        private Animator animator;

        void Awake() {
            animator = GetComponent<Animator>();
        }

        void Start() {
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, true);
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Tab)) {
                ToggleOverlay();
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                HideOverlay();
            }

            var isControlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (Input.GetKeyDown(KeyCode.F1) && !isControlDown) {
                LoadScene(Scenes[0]);
            }

            if (Input.GetKeyDown(KeyCode.F2) && !isControlDown) {
                LoadScene(Scenes[1]);
            }

            if (Input.GetKeyDown(KeyCode.F3) && !isControlDown) {
                LoadScene(Scenes[2]);
            }

            if (Input.GetKeyDown(KeyCode.F4) && !isControlDown) {
                LoadScene(Scenes[3]);
            }

            if (Input.GetKeyDown(KeyCode.F5) && !isControlDown) {
                LoadScene(Scenes[4]);
            }

            if (Input.GetKeyDown(KeyCode.F6) && !isControlDown) {
                LoadScene(Scenes[5]);
            }

            if (Input.GetKeyDown(KeyCode.F7) && !isControlDown) {
                LoadScene(Scenes[6]);
            }

            if (Input.GetKeyDown(KeyCode.F8) && !isControlDown) {
                LoadScene(Scenes[7]);
            }

            if (Input.GetKeyDown(KeyCode.F9) && !isControlDown) {
                LoadScene(Scenes[8]);
            }

            if (Input.GetKeyDown(KeyCode.F10) && !isControlDown) {
                LoadScene(Scenes[9]);
            }

            if (Input.GetKeyDown(KeyCode.F11) && !isControlDown) {
                LoadScene(Scenes[10]);
            }

            if (Input.GetKeyDown(KeyCode.F12) && !isControlDown) {
                LoadScene(Scenes[11]);
            }

            if (Input.GetKeyDown(KeyCode.F1) && isControlDown) {
                LoadScene(Scenes[12]);
            }

            if (Input.GetKeyDown(KeyCode.F2) && isControlDown) {
                LoadScene(Scenes[13]);
            }

            if (Input.GetKeyDown(KeyCode.F3) && isControlDown) {
                LoadScene(Scenes[14]);
            }
            
            if (Input.GetKeyDown(KeyCode.F4) && isControlDown) {
                LoadScene(Scenes[15]);
            }
        }

        public void ToggleOverlay() {
            IsOverlayOpen = !IsOverlayOpen;

            if (IsOverlayOpen) {
                ShowOverlay();
            } else {
                HideOverlay();
            }
        }

        public void ShowOverlay() {
            animator.SetTrigger("Show");
            IsOverlayOpen = true;
        }

        public void HideOverlay() {
            animator.SetTrigger("Hide");
            IsOverlayOpen = false;
        }

        public void LoadScene(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }

        public void Quit() {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
        Application.Quit();
            #endif
        }
    }
}