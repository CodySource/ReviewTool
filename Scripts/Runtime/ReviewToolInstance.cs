using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

namespace CodySource
{
    namespace ReviewTool
    {
        public abstract class ReviewToolInstance : MonoBehaviour
        {
            #region PROPERTIES

            /// <summary>
            /// Configure export properties
            /// </summary>
            public string SQL_URL = "";
            public string SQL_KEY = "";
            public string SQL_HOST = "";
            public string SQL_DB = "";
            public string SQL_USER = "";
            public string SQL_PASS = "";

            /// <summary>
            /// Triggers when the export fails
            /// </summary>
            public UnityEvent<EXPORT_STATUS> onExportFailed = new UnityEvent<EXPORT_STATUS>();

            /// <summary>
            /// Triggers when the export succeeds
            /// </summary>
            public UnityEvent<EXPORT_STATUS> onExportComplete = new UnityEvent<EXPORT_STATUS>();

            #endregion

            #region INTERNAL METHODS

            /// <summary>
            /// Performs the export operation
            /// </summary>
            protected void _Export(string pJSON)
            {
                StartCoroutine(_SQL_Export(
                    new ExportProfile()
                    {
                        sql_db = SQL_DB,
                        sql_host = SQL_HOST,
                        sql_key = SQL_KEY,
                        sql_pass = SQL_PASS,
                        sql_url = SQL_URL,
                        sql_user = SQL_USER
                    }, pJSON
                    ));
            }

            /// <summary>
            /// Performs the actual object eqport
            /// </summary>
            internal IEnumerator _SQL_Export(ExportProfile pProfile, string pJSON)
            {
                WWWForm form = new WWWForm();
                form.AddField("key", $"{pProfile.sql_key}");
                form.AddField("payload", pJSON);
                using (UnityWebRequest www = UnityWebRequest.Post($"https://{pProfile.sql_url}/{name}.php", form))
                {
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(www.error);
                        onExportFailed?.Invoke(EXPORT_STATUS.SQL_Error(www.error));
                    }
                    else
                    {
                        SQL_Repsponse response = JsonUtility.FromJson<SQL_Repsponse>(www.downloadHandler.text);
                        if (response.success)
                        {
                            Debug.Log($"Review Tool Success => {response.success}\t\tTimestamp => {response.submission_success}");
                            onExportComplete?.Invoke(EXPORT_STATUS.SQL_Success(response.submission_success));
                        }
                        else
                        {
                            Debug.Log($"Review Tool Success => {response.success}\t\tError => {response.error}");
                            onExportFailed?.Invoke(EXPORT_STATUS.SQL_Error(response.error));
                        }
                    }
                }
            }

            #endregion

            #region STRUCTS

            [System.Serializable]
            public struct EXPORT_STATUS
            {
                public bool success;
                public string message;
                public static EXPORT_STATUS SQL_Error(string pMessage) =>
                    new EXPORT_STATUS() { success = false, message = pMessage };
                public static EXPORT_STATUS SQL_Success(string pMessage) =>
                    new EXPORT_STATUS() { success = true, message = pMessage };
            }

            [System.Serializable]
            public struct SQL_Repsponse
            {
                public bool success;
                public string error;
                public string session_start;
                public string submission_success;
            }

            [System.Serializable]
            public struct ExportProfile
            {
                public string sql_url;
                public string sql_key;
                public string sql_host;
                public string sql_db;
                public string sql_user;
                public string sql_pass;
            }

            #endregion
        }
    }
}