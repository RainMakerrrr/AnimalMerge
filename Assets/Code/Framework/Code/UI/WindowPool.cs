using System.Collections.Generic;
using UnityEngine.UI;

namespace Framework.Code.UI
{
	public class WindowPool
	{
		readonly Dictionary<WindowType, Graphic> windows;

		public WindowPool(WindowHolder windowHolder)
		{
			windows = new Dictionary<WindowType, Graphic>
			{
				{WindowType.Win, windowHolder.Win},
				{WindowType.Lose, windowHolder.Lose},
				{WindowType.Tutorial, windowHolder.Tutorial}
			};
		}

		public void EnableWindows(params WindowType[] windowTypes)
		{
			foreach (WindowType windowType in windowTypes)
			{
				windows[windowType].gameObject.SetActive(true);
			}
		}

		public void DisableWindows(params WindowType[] windowTypes)
		{
			foreach (WindowType windowType in windowTypes)
			{
				windows[windowType].gameObject.SetActive(false);
			}
		}

		public void DisableAllWindows()
		{
			foreach (Graphic window in windows.Values)
			{
				window.gameObject.SetActive(false);
			}
		}
	}
}