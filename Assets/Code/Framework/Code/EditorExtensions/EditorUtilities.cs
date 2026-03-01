using UnityEditor;
using UnityEngine;

namespace Framework.Code.EditorExtensions
{
	public static class EditorUtilities
	{
#if UNITY_EDITOR
		[MenuItem("Framework / Clear Saves")]
		public static void ClearSaves()
		{
			PlayerPrefs.DeleteAll();

			Debug.Log("Saves are cleared");
		}
#endif
	}
}