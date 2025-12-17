namespace UniGame.Runtime.GameAuth
{
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Localization;

    [Serializable]
    public class GameLoginData
    {
        public string providerName;
        public bool enabled = true;
        public AuthType authType = AuthType.Custom;
        public AssetReferenceSprite icon;
        public LocalizedString title;
        public LocalizedString description;
        
        [SerializeReference]
        public IAuthProviderFactory authFactory;
    }

    public enum AuthType
    {
        Custom,
        EmailLogin,
        Guest,
    }
}