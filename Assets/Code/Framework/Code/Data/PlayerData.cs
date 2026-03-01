using System;

namespace Framework.Code.Data
{
    [Serializable]
    public class PlayerData
    {
        public bool UseTaptic;

        public PlayerData()
        {
            UseTaptic = true;
        }
    }
}