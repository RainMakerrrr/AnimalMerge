using UnityEngine;

namespace Framework.Code.Data
{
	[CreateAssetMenu(fileName = "New Game Data", menuName = "Data / Game Data")]
	public class GameData : ScriptableObject
	{
		[SerializeField] float stateSwitchDelay;

		public float StateSwitchDelay => stateSwitchDelay;
	}
}