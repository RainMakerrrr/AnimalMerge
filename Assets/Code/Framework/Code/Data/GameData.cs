using UnityEngine;
using UnityEngine.Serialization;

namespace Framework.Code.Data
{
	[CreateAssetMenu(fileName = "New Game Data", menuName = "Data / Game Data")]
	public class GameData : ScriptableObject
	{
		[FormerlySerializedAs("stateSwitchDelay")] [SerializeField] private float _stateSwitchDelay;
		[SerializeField] private float _deathAnimationDelay;

		public float StateSwitchDelay => _stateSwitchDelay;

		public float DeathAnimationDelay => _deathAnimationDelay;
	}
}
