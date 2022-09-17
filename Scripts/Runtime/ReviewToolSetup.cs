using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
#endif

namespace CodySource
{
    namespace ReviewTool
    {
        public class ReviewToolSetup : MonoBehaviour
        {

#if UNITY_EDITOR

            public static List<string> markerTypes = new List<string> { "Flag", "Number", "Text" };

            #region INTERNAL METHODS

            /// <summary>
            /// Writes the instance of the new tool script
            /// </summary>
            internal static void _WriteToolInstanceCS(string pName, List<(string id, int type)> pMarkers)
            {
                string _output =
                    "using System.Collections;\n" +
                    "using System.Collections.Generic;\n" +
                    "using UnityEngine;\n" +
                    "using Newtonsoft.Json;\n" +
                    "#if UNITY_EDITOR\n" +
                    "using UnityEditor;\n" +
                    "#endif\n" +
                    "\n" +
                    "namespace CodySource\n" +
                    "{\n" +
                    "\tnamespace ReviewTool\n" +
                    "\t{\n" +
                    $"\t\tpublic class {_SanitizeName(pName)} : ReviewToolInstance\n" +
                    "\t\t{\n" +
                    "\t\t\t#region MARKERS\n" +
                    "\n" +
                    "\t\t\t[MARKERS]\n" +
                    "\n" +
                    "\t\t\t#endregion\n" +
                    "\n" +
                    "\t\t\t#region EXPORT\n" +
                    "\n" +
                    "\t\t\tpublic void Export() {\n" +
                    "\t\t\t\t_Export(JsonConvert.SerializeObject(\n" +
                    "\t\t\t\t\tnew ExportData(){\n" +
                    "\t\t\t\t\t\t[EXPORT_DATA]\n" +
                    "\t\t\t\t\t}));\n" +
                    "\t\t\t}\n" +
                    "\n" +
                    "\t\t\tpublic struct ExportData {\n" +
                    "\t\t\t\t[EXPORT_STRUCT]\n" +
                    "\t\t\t}\n" +
                    "\n" +
                    "\t\t\t#endregion\n" +
                    "\t\t}\n" +
                    "\t}\n" +
                    "}";

                _output = _output
                    .Replace("\t\t\t[MARKERS]\n", _GenerateMarkers(pMarkers))
                    .Replace("\t\t\t\t\t\t[EXPORT_DATA]\n", _GenerateExportData(pMarkers))
                    .Replace("\t\t\t\t[EXPORT_STRUCT]\n", _GenerateExportStruct(pMarkers));

                //  Write file
                Directory.CreateDirectory("./Assets/ReviewTool/");
                File.WriteAllText($"./Assets/ReviewTool/{_SanitizeName(pName)}.cs", _output);

                //  Import the new script
                AssetDatabase.ImportAsset($"Assets/ReviewTool/{_SanitizeName(pName)}.cs");
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// Used to write the necessary php file for the php_sql upload method
            /// </summary>
            internal static void _WriteExportScript(ReviewToolInstance pInstance)
            {
                //  TODO:   Fix bug with this script (not loading pInstance)
                string _output = "<?php\n" +
                    "header('Access-Control-Allow-Origin: *');\n" +
                    $"const projectKey = '{pInstance.SQL_KEY}';\n" +
                    $"const tableName = 'Review_{Application.productName.Replace(" ", "_")}_{Application.version.Replace(".", "_")}';\n" +
                    $"const db_HOST = '{pInstance.SQL_HOST}';\n" +
                    $"const db_NAME = '{pInstance.SQL_DB}';\n" +
                    $"const db_USER = '{pInstance.SQL_USER}';\n" +
                    $"const db_PASS = '{pInstance.SQL_PASS}';\n" +
                    "if (!isset($_POST['key'])) Error('Missing or invalid project key!');\n" +
                    "if (!isset($_POST['payload'])) Error('Missing data!');\n" +
                    "try { $obj = json_decode($_POST['payload']); $submission = json_encode($obj); }\n" +
                    "catch (Exception $e) {Error('Invalid json payload!');}\n" +
                    "if (ConnectToDB()) {\n" +
                    "\tif (VerifyTables()) {\n" +
                    "\t\tif (StoreSubmission($submission)) {\n" +
                    "\t\t\t$mysqli->close();\n" +
                    "\t\t\tSuccess('submission_success', gmdate('Y-m-d H:i:s'));}\n" +
                    "\t\telse Error('An unkown error occured while storing submission.  Check your database permissions.');}\n" +
                    "\telse Error('An unkown error occured while creating/verifying tables.  Check your database permissions.');}\n" +
                    "else Error('Unable to connect to database.');\n" +
                    "function Error($text)\n" +
                    "{\n" +
                    "\t$output = new stdClass;\n" +
                    "\t$output->success = false;\n" +
                    "\t$output->error = $text;\n" +
                    "\tdie(json_encode($output));\n" +
                    "}\n" +
                    "function Success()\n" +
                    "{\n" +
                    "\t$output = new stdClass;\n" +
                    "\t$output->success = true;\n" +
                    "\t$argCount = func_num_args();\n" +
                    "\tif ($argCount % 2 != 0) return;\n" +
                    "\t$args = func_get_args();\n" +
                    "\tfor ($i = 0; $i < $argCount; $i += 2)\n" +
                    "\t{\n" +
                    "\t\t$arg = func_get_arg($i);\n" +
                    "\t\t$val = func_get_arg($i + 1);\n" +
                    "\t\t$output->$arg = $val;\n" +
                    "\t}\n" +
                    "\tdie(json_encode($output));\n" +
                    "}\n" +
                    "$mysqli; $timestamp;\n" +
                    "function ConnectToDB()\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\t$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);\n" +
                    "\tif ($mysqli->connect_errno)\n" +
                    "{\n" +
                    "\terror_log('Connect Error: '.$mysqli->connect_error,0);\n" +
                    "\treturn false;\n" +
                    "}\n" +
                    "\t$timestamp = date(DATE_RFC3339);\n" +
                    "\treturn true;\n" +
                    "}\n" +
                    "function VerifyTables()\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\tif ($mysqli->query('CREATE TABLE IF NOT EXISTS '.tableName.' (Submission VARCHAR(1023)); ')) return true;\n" +
                    "\terror_log('Verify Tables Error: '.$mysqli->error,0);\n" +
                    "\treturn false;\n" +
                    "}\n" +
                    "function StoreSubmission($pText)\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\tif ($mysqli->query('INSERT INTO '.tableName.' (Submission) VALUES(\\''.$pText.'\\');')) return true;\n" +
                    "\terror_log('Store Submission Error: '.$mysqli->error,0);\n" +
                    "\treturn false;\n" +
                    "}\n" +
                    "?>";

                //  Write file
                Directory.CreateDirectory("./Assets/ReviewTool/");
                File.WriteAllText($"./Assets/ReviewTool/{pInstance.name}.php", _output);
            }

            /// <summary>
            /// Sanitizes a name string
            /// </summary>
            internal static string _SanitizeName(string pName) => Regex.Replace(Regex.Replace(pName, @"\W+", ""), @"^\d+", "");

            internal static string _FriendlyToType(string pFriendly)
            {
                return pFriendly switch
                {
                    "Flag" => "bool",
                    "Number" => "float",
                    "Text" => "string",
                    _ => ""
                };
            }

            internal static string _TypeToFriendly(string pType)
            {
                return pType switch
                {
                    "System.Bool" => "Flag",
                    "System.Single" => "Number",
                    "System.String" => "Text",
                    _ => ""
                };
            }

            /// <summary>
            /// Generates the markers and their accessors / mutators
            /// </summary>
            private static string _GenerateMarkers(List<(string id, int type)> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                pMarkers.ForEach(m => {
                    string _type = _FriendlyToType(markerTypes[m.type]);
                    _out += 
                    $"\t\t\t//\t{m.id} Accessors / Mutators / Special Methods \n" + 
                    $"\t\t\tpublic {_type} {m.id};\n" +
                    $"\t\t\tpublic {_type} Get_{m.id}() => {m.id};\n" +
                    $"\t\t\tpublic void Set_{m.id}({_type} pVal) => {m.id} = pVal;\n";
                    _out += _type switch
                    {
                        "bool" => $"\t\t\tpublic void Toggle_{m.id}() => {m.id} = !{m.id};\n",
                        "float" => $"\t\t\tpublic void Add_{m.id}(float pVal) => {m.id} += pVal;\n",
                        "string" => $"\t\t\tpublic void Filter_{m.id}(string pVal) => {m.id} = _Filter({m.id});\n",
                        _ => ""
                    };
                    _out += "\n";
                });
                return _out;
            }

            /// <summary>
            /// Generates the export class for the instance
            /// </summary>
            private static string _GenerateExportData(List<(string id, int type)> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                pMarkers.ForEach(m => { _out += $"\t\t\t\t\t\t{m.id} = Get_{m.id}(),\n"; });
                return _out;
            }

            /// <summary>
            /// Generates the export class for the instance
            /// </summary>
            private static string _GenerateExportStruct(List<(string id, int type)> pMarkers)
            {
                if (pMarkers == null || pMarkers.Count == 0) return "\n";
                string _out = "";
                pMarkers.ForEach(m => { _out += $"\t\t\t\tpublic {_FriendlyToType(markerTypes[m.type])} {m.id};\n"; });
                return _out;
            }

            #endregion

#endif

        }
    }
}