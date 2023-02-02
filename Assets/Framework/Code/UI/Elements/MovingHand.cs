using UnityEngine;

namespace Framework.Code.UI.Elements
{
	public class MovingHand : MonoBehaviour
	{
		[SerializeField] RectTransform handRect;
		[SerializeField] RectTransform arrowRect;
		[SerializeField] float moveSpeed;

		float maximumX;
		float minimumX;


		void Start()
		{
			maximumX = arrowRect.rect.max.x;
			minimumX = arrowRect.rect.min.x;
		}

		void Update() => MoveHand();

		void MoveHand()
		{
			handRect.anchoredPosition += new Vector2(moveSpeed * Time.deltaTime, 0f);

			if (handRect.anchoredPosition.x > maximumX)
			{
				moveSpeed *= -1f;
				handRect.anchoredPosition = new Vector2(maximumX, handRect.anchoredPosition.y);
			}

			if (handRect.anchoredPosition.x < minimumX)
			{
				moveSpeed = Mathf.Abs(moveSpeed);
				handRect.anchoredPosition = new Vector2(minimumX, handRect.anchoredPosition.y);
			}
		}
	}
}