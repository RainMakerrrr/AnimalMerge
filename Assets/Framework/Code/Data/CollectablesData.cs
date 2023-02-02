using System;

namespace Framework.Code.Data
{
	[Serializable]
	public class CollectablesData
	{
		public event Action AmountChanged;

		[NonSerialized] public int LevelAmount;

		public int Amount;


		public void Add(int amount)
		{
			LevelAmount += amount;
			Amount += amount;

			AmountChanged?.Invoke();
		}

		public void ResetAmount() => Amount -= LevelAmount;
	}
}