using UnityEngine;

namespace Framework.Code.UI.Elements
{
	[RequireComponent(typeof(RectTransform))]
	public class SafeArea : MonoBehaviour
	{
		private RectTransform _rectTransform;
		private Rect _appliedSafeArea;
		private Vector2Int _appliedScreenSize;
		private ScreenOrientation _appliedOrientation;

		private void Awake()
		{
			_rectTransform = (RectTransform)transform;

			Apply();
		}

		private void OnEnable() => Apply();

		private void Update()
		{
			if (IsUpToDate())
				return;

			Apply();
		}

		private bool IsUpToDate() =>
			_appliedSafeArea == Screen.safeArea
			&& _appliedScreenSize.x == Screen.width
			&& _appliedScreenSize.y == Screen.height
			&& _appliedOrientation == Screen.orientation;

		private void Apply()
		{
			if (_rectTransform == null)
				return;

			_appliedSafeArea = Screen.safeArea;
			_appliedScreenSize = new Vector2Int(Screen.width, Screen.height);
			_appliedOrientation = Screen.orientation;

			if (_appliedScreenSize.x <= 0 || _appliedScreenSize.y <= 0)
				return;

			var anchorMin = _appliedSafeArea.position;
			var anchorMax = _appliedSafeArea.position + _appliedSafeArea.size;

			anchorMin.x /= _appliedScreenSize.x;
			anchorMin.y /= _appliedScreenSize.y;
			anchorMax.x /= _appliedScreenSize.x;
			anchorMax.y /= _appliedScreenSize.y;

			_rectTransform.anchorMin = anchorMin;
			_rectTransform.anchorMax = anchorMax;
			_rectTransform.offsetMin = Vector2.zero;
			_rectTransform.offsetMax = Vector2.zero;
		}
	}
}
