namespace UniGame.Runtime.GameAuth
{
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public interface IGameAuthProvider 
    {
        /// <summary>
        /// is user authenticated
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// is registration supported by this provider
        /// </summary>
        bool AllowRegisterAccount { get; }
        
        /// <summary>
        /// is restoring account supported by this provider
        /// </summary>
        bool AllowRestoreAccount { get; }

        UniTask<AuthProviderResult> LoginAsync(ILoginContext context,CancellationToken cancellationToken = default);
        
        UniTask<SignOutResult> SignOutAsync();
    }
}