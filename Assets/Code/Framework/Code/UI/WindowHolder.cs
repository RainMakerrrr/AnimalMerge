using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Framework.Code.UI
{
	public class WindowHolder : MonoBehaviour
	{
		[FormerlySerializedAs("win")] [SerializeField] private Graphic _win;
		[FormerlySerializedAs("lose")] [SerializeField] private Graphic _lose;
		[FormerlySerializedAs("tutorial")] [SerializeField] private Graphic _tutorial;
		[SerializeField] private Graphic _campaignVictory;

		public Graphic Win => _win;
		public Graphic Lose => _lose;
		public Graphic Tutorial => _tutorial;
		public Graphic CampaignVictory => _campaignVictory;
	}
}
