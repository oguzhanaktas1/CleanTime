using UnityEditor;
using UnityEngine;
using CleanTime.Cleaning.Presentation;

namespace CleanTime.Editor.ContentTools
{
    [CustomEditor(typeof(CleaningTargetAuthoring))]
    public sealed class CleaningTargetAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var target = (CleaningTargetAuthoring)this.target;

            EditorGUILayout.HelpBox(
                "The stable id is referenced by mid-job saves. Only regenerate it for a freshly duplicated object.",
                MessageType.Info);

            DrawDefaultInspector();

            if (target.IsValid(out var problem))
                return;

            EditorGUILayout.HelpBox("Not usable yet: " + problem + ".", MessageType.Warning);

            if (string.IsNullOrWhiteSpace(target.StableInstanceId) && GUILayout.Button("Generate stable id"))
            {
                Undo.RecordObject(target, "Generate cleaning target id");
                target.AssignNewStableId();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
