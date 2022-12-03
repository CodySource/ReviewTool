using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
#endif

namespace CodySource
{
    namespace ReviewTool
    {
        public class ReviewToolSetup : MonoBehaviour
        {

#if UNITY_EDITOR

            public static List<string> markerTypes = new List<string> { "bool", "float", "string" };

            #region INTERNAL METHODS

            /// <summary>
            /// Used to write the necessary php file for the php_sql upload method
            /// </summary>
            internal static void _WriteExportScript(ReviewToolInstance pInstance, List<ReviewToolMarker> pMarkers)
            {

                //  Breakout if the instance hasn't been created yet
                if (pInstance == null) return;

                string _headers = "";
                pMarkers.ForEach(m => _headers += m.id + ",");
                _headers = _headers != "" ? _headers.Substring(0, _headers.Length - 1) : "";

                string _output = File.ReadAllText("Packages/com.codysource.reviewtool/Scripts/Runtime/ReviewToolTemplate.php")
                    .Replace("[PROJECT_KEY]", pInstance.SQL_KEY)
                    .Replace("[TABLE_NAME]", $"{Application.productName.Replace(" ", "_")}_{Application.version.Replace(".", "_").Replace("[", "").Replace("]", "").Split('-')[0]}_Review")
                    .Replace("[DB_HOST]", pInstance.SQL_HOST)
                    .Replace("[DB_NAME]", pInstance.SQL_DB)
                    .Replace("[DB_USER]", pInstance.SQL_USER)
                    .Replace("[DB_PASS]", pInstance.SQL_PASS)
                    .Replace("[HEADERS]", _headers)
                    .Replace("[NAME]", pInstance.name);

                //  Write file
                Directory.CreateDirectory("./Assets/ReviewTool/");
                File.WriteAllText($"./Assets/ReviewTool/{pInstance.name}.php", _output);
            }

            /// <summary>
            /// Writes the instance of the new tool script
            /// </summary>
            internal static void _WriteToolInstanceCS(string pName, List<ReviewToolMarker> pMarkers)
            {
                string _output = File.ReadAllText("Packages/com.codysource.reviewtool/Scripts/Runtime/ReviewToolTemplate.cs");

                _output = _output
                    .Replace("ReviewToolTemplate", _SanitizeName(pName))
                    .Replace("//\t[MARKERS]", _GenerateMarkers(pMarkers))
                    .Replace("//\t[EXPORT_DATA]", _GenerateExportData(pMarkers))
                    .Replace("//\t[EXPORT_STRUCT]", _GenerateExportStruct(pMarkers));

                //  Write file
                Directory.CreateDirectory("./Assets/ReviewTool/");
                File.WriteAllText($"./Assets/ReviewTool/{_SanitizeName(pName)}.cs", _output);

                //  Import the new script
                AssetDatabase.ImportAsset($"Assets/ReviewTool/{_SanitizeName(pName)}.cs");
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// Sanitizes a name string
            /// </summary>
            internal static string _SanitizeName(string pName) => Regex.Replace(Regex.Replace(pName, @"\W+", ""), @"^\d+", "");

            /// <summary>
            /// Generates the markers and their accessors / mutators
            /// </summary>
            private static string _GenerateMarkers(List<ReviewToolMarker> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                int _count = 0;
                pMarkers.ForEach(m => {
                    _count++;
                    _out += 
                    $"\t\t\t//\t{m.id} Accessors / Mutators / Special Methods \n" + 
                    $"\t\t\tpublic {m.type} {m.id};\n" +
                    $"\t\t\tpublic {m.type} Get_{m.id}() => {m.id};\n" +
                    $"\t\t\tpublic void Set_{m.id}({m.type} pVal) => {m.id} = pVal;\n";
                    _out += m.type switch
                    {
                        "bool" => $"\t\t\tpublic void Toggle_{m.id}() => {m.id} = !{m.id};\n",
                        "float" => $"\t\t\tpublic void Add_{m.id}(float pVal) => {m.id} += pVal;\n",
                        _ => ""
                    };
                    _out += (_count < pMarkers.Count) ? "\n" : "";
                });
                return _out;
            }

            /// <summary>
            /// Generates the export class for the instance
            /// </summary>
            private static string _GenerateExportData(List<ReviewToolMarker> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                int _count = 0;
                pMarkers.ForEach(m => {
                    _count++;
                    _out += $"\t\t\t\t\t\t{m.id} = Get_{m.id}()," + (_count < pMarkers.Count ? "\n" : ""); 
                });
                return _out;
            }

            /// <summary>
            /// Generates the export class for the instance
            /// </summary>
            private static string _GenerateExportStruct(List<ReviewToolMarker> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                int _count = 0;
                pMarkers.ForEach(m => {
                    _count++; 
                    _out += $"\t\t\t\tpublic {m.type} {m.id};" + (_count < pMarkers.Count ? "\n" : ""); 
                });
                return _out;
            }

            #endregion

#endif

        }
    }
}