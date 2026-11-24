using Rubber.World;
using UnityEditor;
using UnityEngine;

namespace Rubber.EditorTools
{
    [CustomEditor(typeof(VillaSlidingDoor))]
    public sealed class VillaSlidingDoorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Play mode: test this door here. Sliding: local left, width × ratio. Hinged: local Y rotation around the authored hinge pivot. Gameplay: Open / Close / Toggle.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var door = (VillaSlidingDoor)target;
                if (GUILayout.Button("Open")) door.Open();
                if (GUILayout.Button("Close")) door.Close();
                if (GUILayout.Button("Toggle")) door.Toggle();
            }
        }
    }
}
