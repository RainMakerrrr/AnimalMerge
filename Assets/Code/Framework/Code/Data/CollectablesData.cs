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

		public void RevertLevelAmount()
		{
			Amount -= LevelAmount;
			LevelAmount = 0;

			AmountChanged?.Invoke();
		}

		public void Clear()
		{
			Amount = 0;
			LevelAmount = 0;

			AmountChanged?.Invoke();
		}
	}
}