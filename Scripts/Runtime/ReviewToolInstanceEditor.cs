using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
#endif

namespace CodySource
{
    namespace ReviewTool
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(ReviewToolInstance), true)]
        public class ReviewToolInstanceEditor : Editor
        {
            string _newMarkerName = "";
            int _newMarkerType = 0;
            bool _isLoading = false;
            List<ReviewToolMarker> markers = new List<ReviewToolMarker> { };
            Dictionary<string, SerializedProperty> props = new Dictionary<string, SerializedProperty>();
            bool exportExpanded = false;
            bool markersExpanded = true;
            public override void OnInspectorGUI()
            {
                ReviewToolInstance _instance = (ReviewToolInstance)target;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (exportExpanded)
                {
                    if (GUILayout.Button("EXPORT CONFIG", EditorStyles.foldoutHeader)) exportExpanded = false;
                    GUILayout.Space(3f);
                    foreach (KeyValuePair<string, SerializedProperty> prop in props)
                    {
                        //  TODO:   If there are changes, re-write files
                        if (!markers.Exists(m => m.id == prop.Key) && !prop.Key.StartsWith("_"))
                        {
                            if (prop.Key.Contains("PASS"))
                            {
                                prop.Value.stringValue = EditorGUILayout.PasswordField(prop.Key, prop.Value.stringValue);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(prop.Value);
                            }
                            GUILayout.Space(3f);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("EXPORT CONFIG", EditorStyles.foldoutHeader)) exportExpanded = true;
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10f);


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (markersExpanded)
                {
                    if (GUILayout.Button("MARKER CONFIG", EditorStyles.foldoutHeader)) markersExpanded = false;
                    GUILayout.Space(3f);

                    if (!EditorApplication.isCompiling)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Marker Name: ", GUILayout.MaxWidth(80f));
                        _newMarkerName = GUILayout.TextField(ReviewToolSetup._SanitizeName(_newMarkerName));
                        _newMarkerType = Mathf.Max(EditorGUILayout.Popup(_newMarkerType, ReviewToolSetup.markerTypes.ToArray(), GUILayout.MaxWidth(80f)));
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.green;
                        //  TODO:   Add protections for marker name text box
                        if (GUILayout.Button((_newMarkerName != "")? "Add Marker" : "Save"))
                        {
                            if (!_isLoading)
                            {
                                if (_newMarkerName != "")
                                {
                                    markers.Add(new ReviewToolMarker() { id = _newMarkerName, type = ReviewToolSetup.markerTypes[_newMarkerType] });
                                }
                                _isLoading = true;
                                _newMarkerName = "";
                                _newMarkerType = 0;
                                ReviewToolSetup._WriteExportScript(_instance, markers);
                                ReviewToolSetup._WriteToolInstanceCS(ReviewToolSetup._SanitizeName(_instance.gameObject.name), markers);
                            }
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(10f);

                        if (!_isLoading)
                        {
                            ReviewToolMarker _remove = new ReviewToolMarker() { id = "", type = "" };
                            markers.ForEach(m =>
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUI.backgroundColor = Color.red;
                                if (GUILayout.Button("X", GUILayout.MaxWidth(20f))) _remove = m;
                                GUI.backgroundColor = Color.white;
                                GUILayout.Label($"({m.type})", GUILayout.MaxWidth(60f));
                                EditorGUILayout.PropertyField(props[m.id]);
                                EditorGUILayout.EndHorizontal();
                                GUILayout.Space(10f);
                            });
                            if (_remove.type != "")
                            {
                                markers.Remove(_remove);
                                _isLoading = true;
                                ReviewToolSetup._WriteExportScript(_instance, markers);
                                ReviewToolSetup._WriteToolInstanceCS(ReviewToolSetup._SanitizeName(_instance.gameObject.name), markers);
                            }
                        }
                        else GUILayout.Label("Loading...");
                    }
                    else
                    {
                        GUILayout.Label("Compiling...");
                        _isLoading = false;
                    }
                    GUILayout.Space(3f);
                }
                else
                {
                    if (GUILayout.Button("MARKER CONFIG", EditorStyles.foldoutHeader)) markersExpanded = true;
                }
                EditorGUILayout.EndVertical();


                serializedObject.ApplyModifiedProperties();

            }

            public void OnEnable()
            {
                FieldInfo[] _fields = System.Type.GetType(
                    $"CodySource.ReviewTool.{((ReviewToolInstance)target).name}" + ",Assembly-CSharp")
                    .GetFields();
                markers.Clear();
                props.Clear();
                for (int i = 0; i < _fields.Length; i++)
                {
                    props.Add(_fields[i].Name, serializedObject.FindProperty(_fields[i].Name));
                    if (!_fields[i].Name.StartsWith("SQL") && !_fields[i].FieldType.ToString().Contains("UnityEvent"))
                    {
                        markers.Add(new ReviewToolMarker()
                        {
                            id = _fields[i].Name,
                            type = _fields[i].FieldType.ToString().Replace("System.", "").ToLower()
                                .Replace("single","float")
                                .Replace("boolean","bool")
                        });
                    }
                }
            }
        }
#else
        public class ReviewToolInstanceEditor {}
#endif
    }
}