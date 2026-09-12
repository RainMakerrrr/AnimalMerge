using System;
using System.Collections.Generic;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;

namespace Code.Levels
{
    public class LevelSetProvider : ILevelSetProvider
    {
        private readonly IAssetProvider _assetProvider;

        private LevelSetLibrary _library;
        private LevelSet _selectedSet;

        public LevelSetProvider(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public event Action<LevelSet> ActiveSetChanged;

        public LevelSet ActiveSet
        {
            get
            {
                if (_selectedSet != null)
                    return _selectedSet;

                LevelSetLibrary library = Library;

                return library == null ? null : library.DefaultSet;
            }
        }

        public IReadOnlyList<LevelSet> AvailableSets
        {
            get
            {
                LevelSetLibrary library = Library;

                return library == null ? Array.Empty<LevelSet>() : library.Sets;
            }
        }

        public void SetActiveSet(LevelSet set)
        {
            if (set == null)
            {
                Debug.LogError("[LevelSetProvider] Cannot activate an empty level set");
                return;
            }

            if (ActiveSet == set)
                return;

            WarnIfNotShipped(set);

            _selectedSet = set;
            ActiveSetChanged?.Invoke(set);
        }

        private LevelSetLibrary Library
        {
            get
            {
                if (_library == null)
                    _library = _assetProvider.Load<LevelSetLibrary>(AssetPath.LEVEL_SET_LIBRARY);

                return _library;
            }
        }

        private void WarnIfNotShipped(LevelSet set)
        {
            IReadOnlyList<LevelSet> shippedSets = AvailableSets;

            for (var index = 0; index < shippedSets.Count; index++)
            {
                if (shippedSets[index] == set)
                    return;
            }

            Debug.LogWarning(
                $"[LevelSetProvider] '{set.name}' is not listed in {AssetPath.LEVEL_SET_LIBRARY}, " +
                "so it will be missing from a player build. Register it in Tools/AnimalMerge/Level Sets");
        }
    }
}
