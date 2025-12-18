namespace UniGame.Runtime.GameAuth
{
    using Core.Runtime;
    using Cysharp.Threading.Tasks;

    public interface IAuthProviderFactory
    {
        UniTask<IGameAuthProvider> CreateAsync(string id,IContext context);
    }
}