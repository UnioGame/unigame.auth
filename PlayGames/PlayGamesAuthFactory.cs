namespace UniGame.Runtime.GameAuth.FirebaseEmail
{
    using System;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using PlayGames;

    [Serializable]
    public class PlayGamesAuthFactory: IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id,IContext context)
        {
            return new PlayGamesAuthProvider();
        }
    }
}