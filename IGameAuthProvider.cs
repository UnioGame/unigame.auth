namespace UniGame.Runtime.GameAuth
{
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public interface IGameAuthProvider 
    {
        /// <summary>
        /// is registration supported by this provider
        /// </summary>
        bool AllowRegisterAccount { get; }
        
        /// <summary>
        /// is restoring account supported by this provider
        /// </summary>
        bool AllowRestoreAccount { get; }
        
        bool CheckAuthContext(IAuthContext context);

        UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default);
        
        UniTask<AuthProviderResult> RestoreAsync(IAuthContext context,CancellationToken cancellationToken = default);
        
        UniTask<AuthSignOutResult> SignOutAsync();
    }
}