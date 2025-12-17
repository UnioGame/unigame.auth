namespace UniGame.Runtime.GameAuth.FirebaseEmail
{
    using System;
    using Cysharp.Threading.Tasks;
    using PlayGames;

    [Serializable]
    public class UnityPlayGamesAuthFactory: IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id)
        {
            return new UnityPlayGamesAuthProvider(id);
        }
    }
}