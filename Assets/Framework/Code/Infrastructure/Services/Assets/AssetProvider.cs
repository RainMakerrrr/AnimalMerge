using UnityEngine;

namespace Framework.Code.Infrastructure.Services.Assets
{
	public class AssetProvider : IAssetProvider
	{
		public T Load<T>(string path) where T : Object => Resources.Load<T>(path);

		T[] IAssetProvider.LoadCollection<T>(string path) => Resources.LoadAll<T>(path);

		public void Clear() => Resources.UnloadUnusedAssets();
	}
}