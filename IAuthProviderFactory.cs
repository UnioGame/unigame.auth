namespace UniGame.Runtime.GameAuth
{
    using Cysharp.Threading.Tasks;

    public interface IAuthProviderFactory
    {
        UniTask<IGameAuthProvider> CreateAsync(string id);
    }
}