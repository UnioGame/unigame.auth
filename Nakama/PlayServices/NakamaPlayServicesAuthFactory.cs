namespace PlayGames.NakamaGoogle
{
    using System;
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;
    using UniGame.Runtime.GameAuth;

    [Serializable]
    public class NakamaPlayServicesAuthFactory: IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id,IContext context)
        {
            return new NakamaPlayServicesAuthProvider(id);
        }
    }
}