namespace UniGame.Runtime.GameAuth
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/AuthService/Auth Configuration", fileName = "GameAuthConfig")]
    public class GameAuthConfigurationAsset : ScriptableObject
    {
        [InlineProperty]
        [HideLabel]
        public GameAuthConfiguration configuration = new();
    }
}