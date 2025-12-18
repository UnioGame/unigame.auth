namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using UniGame.GameFlow.Runtime;

    public interface IUniGameAuthService : IGameService
    {
        Observable<GameAuthResult> AuthAction { get; }
        
        ReadOnlyReactiveProperty<GameAuthResult> AuthStatus { get; }

        void RegisterProvider(string id, GameLoginData data, IGameAuthProvider provider);
                
        GameLoginData GetAuthProviderData(string id);
        
        IEnumerable<GameLoginData> GetAvailableAuth();

        UniTask<SignOutResult> SignOutAsync();

        UniTask<GameAuthResult> RestoreAuthAsync(CancellationToken ct = default);
        
        UniTask<GameAuthResult> SignInAsync(string id,IAuthContext loginContext,CancellationToken ct = default);
        
        UniTask<ResetCredentialResult> ResetCredentialAsync(string id,IAuthContext loginContext);
        
        UniTask<GameRegisterResult> RegisterAsync(string id,IAuthContext loginContext,CancellationToken ct = default);

    }
}