using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Framework.Code.VariativeComponents
{
	public class ProgressBar : MonoBehaviour
	{
		[SerializeField] Slider progressBar;
		[SerializeField] Transform player;
		[SerializeField] Transform levelEnd;

		float distance;

		void Start() => distance = GetDistance();

		void Update() => UpdateProgressBar();

		float GetDistance()
		{
			if (player == null || levelEnd == null)
			{
				Debug.LogError("Player or finish line are not assigned");
				return 0f;
			}

			return (levelEnd.transform.position - player.transform.position).sqrMagnitude;
		}

		void UpdateProgressBar()
		{
			if (player == null || levelEnd == null)
			{
				Debug.LogError("Player or finish line are not assigned");
				return;
			}

			if (player.transform.position.z > levelEnd.transform.position.z) return;

			float newDistance = GetDistance();
			float progressValue = Mathf.InverseLerp(distance, 0f, newDistance);

			progressBar.value = progressValue;
		}
	}
}