using System.Collections.Generic;
using UnityEngine.UI;

namespace Framework.Code.UI
{
	public class WindowPool
	{
		private readonly Dictionary<WindowType, Graphic> _windows;

		public WindowPool(WindowHolder windowHolder)
		{
			_windows = new Dictionary<WindowType, Graphic>
			{
				{WindowType.Win, windowHolder.Win},
				{WindowType.Lose, windowHolder.Lose},
				{WindowType.Tutorial, windowHolder.Tutorial},
				{WindowType.CampaignVictory, windowHolder.CampaignVictory}
			};
		}

		public void EnableWindows(params WindowType[] windowTypes)
		{
			foreach (WindowType windowType in windowTypes)
			{
				_windows[windowType].gameObject.SetActive(true);
			}
		}

		public void DisableWindows(params WindowType[] windowTypes)
		{
			foreach (WindowType windowType in windowTypes)
			{
				_windows[windowType].gameObject.SetActive(false);
			}
		}

		public void DisableAllWindows()
		{
			foreach (Graphic window in _windows.Values)
			{
				window.gameObject.SetActive(false);
			}
		}
	}
}
