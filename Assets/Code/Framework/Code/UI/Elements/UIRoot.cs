using System.Collections.Generic;
using UnityEngine;

namespace Framework.Code.UI.Elements
{
	public class UIRoot : MonoBehaviour
	{
		public IReadOnlyCollection<IViewUpdatable> ViewUpdaters => GetComponentsInChildren<IViewUpdatable>();
	}
}