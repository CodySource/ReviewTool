using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodySource
{
	namespace ReviewTool
	{
		public class ReviewToolTemplate : ReviewToolInstance
		{
			#region MARKERS

//	[MARKERS]
			#endregion

			#region EXPORT

			public void Export() {
				_Export(JsonConvert.SerializeObject(
					new ExportData(){
//	[EXPORT_DATA]
					}));
			}

			public struct ExportData {
//	[EXPORT_STRUCT]
			}

			#endregion
		}
	}
}