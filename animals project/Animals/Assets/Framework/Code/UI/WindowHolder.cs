using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Framework.Code.UI
{
	public class WindowHolder : MonoBehaviour
	{
		[SerializeField] Graphic win;
		[SerializeField] Graphic lose;
		[SerializeField] Graphic tutorial;

		public Graphic Win => win;
		public Graphic Lose => lose;
		public Graphic Tutorial => tutorial;
	}
}