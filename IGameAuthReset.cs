namespace UniGame.Runtime.GameAuth
{
    using Cysharp.Threading.Tasks;

    public interface IGameAuthReset
    {
        UniTask<ResetCredentialResult> ResetAuthAsync(ILoginContext context);
    }
    
    public struct ResetCredentialResult
    {
        public bool success;
        public string error;
    }
}