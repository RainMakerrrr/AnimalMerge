using System;
using UnityEngine.Serialization;

namespace Framework.Code.Data
{
	[Serializable]
	public class PlayerProgress
	{
		public int Level;
		public CollectablesData Collectables;

		public PlayerProgress()
		{
			Level = 1;
			Collectables = new CollectablesData();
		}
	}
}