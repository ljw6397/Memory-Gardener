using UnityEditor;

#if UNITY_EDITOR
namespace PixelUI {
    [CustomEditor(typeof(SlotBar))]
    public class SlotBarEditor : Editor {
        private SlotBar spawner;

        private void OnEnable() {
            spawner = (SlotBar) target;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                spawner.UpdateSlots();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif