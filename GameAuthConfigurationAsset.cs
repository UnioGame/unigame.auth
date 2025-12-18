namespace UniGame.Runtime.GameAuth
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/AuthService/Auth Configuration", fileName = "GameAuthConfig")]
    public class GameAuthConfigurationAsset : ScriptableObject
    {
        [InlineProperty]
        [HideLabel]
        public GameAuthConfiguration configuration = new();


        

        public static IEnumerable<string> AuthProviders()
        {
#if UNITY_EDITOR
            var configuration = AssetEditorTools.GetAsset<GameAuthConfigurationAsset>();

            foreach (var configurationLoginProvider in configuration.configuration.loginProviders)
            {
                yield return configurationLoginProvider.providerName;
            }
            
#endif
            yield break;
        }
    }
}