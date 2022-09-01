using UnityEngine;

namespace Framework.Code.Infrastructure.Services.Assets
{
	public interface IAssetProvider
	{
		T Load<T>(string path) where T : Object;
		T[] LoadCollection<T>(string path) where T : Object;
		void Clear();
	}
}