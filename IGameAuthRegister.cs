namespace UniGame.Runtime.GameAuth
{
    using Cysharp.Threading.Tasks;

    public interface IGameAuthRegister
    {
        UniTask<AuthProviderResult> RegisterAsync(ILoginContext context);
    }
}