namespace UniGame.Runtime.GameAuth
{
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public interface IGameAuthRegister
    {
        UniTask<AuthProviderResult> RegisterAsync(IAuthContext context, CancellationToken cancellationToken = default);
    }
}