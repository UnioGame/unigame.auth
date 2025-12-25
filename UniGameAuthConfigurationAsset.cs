namespace UniGame.Runtime.GameAuth
{
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/AuthService/Auth Configuration", fileName = "GameAuthConfig")]
    public class UniGameAuthConfigurationAsset : ScriptableObject
    {

#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public GameAuthConfiguration configuration = new();
        
        public static IEnumerable<string> AuthProviders()
        {
#if UNITY_EDITOR
            var configuration = AssetEditorTools.GetAsset<UniGameAuthConfigurationAsset>();

            foreach (var configurationLoginProvider in configuration.configuration.loginProviders)
            {
                yield return configurationLoginProvider.providerName;
            }
            
#endif
            yield break;
        }
    }
}