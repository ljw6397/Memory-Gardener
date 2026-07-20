using UnityEditor;

#if UNITY_EDITOR
namespace PixelUI {
    [CustomEditor(typeof(List))]
    public class ListEditor : Editor {
        private List spawner;

        private void OnEnable() {
            spawner = (List) target;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                spawner.UpdateItems();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif