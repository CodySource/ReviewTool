using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CodySource
{
    namespace ReviewTool
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(ReviewToolSetup))]
        public class ReviewToolSetupEditor : Editor
        {
            private bool _isSettingUp = false;

            public override void OnInspectorGUI()
            {
                if (!_isSettingUp)
                {
                    if (GUILayout.Button("Setup"))
                    {
                        ReviewToolSetup _setup = (ReviewToolSetup)target;
                        ReviewToolSetup._WriteToolInstanceCS(ReviewToolSetup._SanitizeName(_setup.gameObject.name),
                            new List<(string id, string type)>() {});
                        _isSettingUp = true;
                    }
                }
                else
                {
                    if (!EditorApplication.isCompiling)
                    {
                        //  Compilation is complete
                        ReviewToolSetup _setup = (ReviewToolSetup)target;
                        string _name = ReviewToolSetup._SanitizeName(_setup.gameObject.name);
                        System.Type _type = System.Type.GetType($"CodySource.ReviewTool.{_name}" + ",Assembly-CSharp");
                        ReviewToolInstance _instance = (ReviewToolInstance)_setup.gameObject.AddComponent(_type);
                        ReviewToolSetup._WriteExportScript(_instance, new List<(string id, string type)>());
                        DestroyImmediate(_setup);
                    }
                    else GUILayout.Label("Setup is being performed...");
                }
            }
        }
#else
        public class ReviewToolSetupEditor {}
#endif
    }
}