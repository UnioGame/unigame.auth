namespace UniGame.Runtime.GameAuth
{
    using Cysharp.Threading.Tasks;

    public interface IGameAuthReset
    {
        UniTask<ResetCredentialResult> ResetAuthAsync(IAuthContext context);
    }
    
    public struct ResetCredentialResult
    {
        public bool success;
        public string error;
    }
}