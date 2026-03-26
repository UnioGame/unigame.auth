namespace UniGame.Runtime.GameAuth
{
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Localization;
    
#if  UNITY_EDITOR
    using UniModules.Editor;
#endif

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    [Serializable]
    public class GameLoginData
    {
        public string providerName;
        public bool enabled = true;
        public AuthType authType = AuthType.Custom;
        public AssetReferenceSprite icon;
        public LocalizedString title;
        public LocalizedString description;

#if ODIN_INSPECTOR
        [InlineButton(nameof(OpenScript), "Open Script")]
#endif
        [SerializeReference]
        public IAuthProviderFactory authFactory;

        
        public void OpenScript()
        {
#if  UNITY_EDITOR
            if (authFactory == null) return;
            var type = authFactory.GetType();
            type.OpenScript();
#endif
        }

    }

    public enum AuthType
    {
        Custom,
        EmailLogin,
        Guest,
    }
}