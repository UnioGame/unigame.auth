namespace UniGame.Runtime.GameAuth.FacebookAuth
{
    using System;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;

    [Serializable]
    public class FacebookAuthFactory: IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id,IContext context)
        {
            return new FacebookAuthProvider();
        }
    }
}