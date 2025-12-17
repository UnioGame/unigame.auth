namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using R3;
    using UniGame.GameFlow.Runtime;

    public interface IGameAuthService : IGameService
    {
        Observable<GameAuthResult> AuthAction { get; }
        
        ReadOnlyReactiveProperty<GameAuthResult> AuthStatus { get; }

        void RegisterProvider(string id, GameLoginData data, IGameAuthProvider provider);
                
        GameLoginData GetAuthProviderData(string id);
        
        IEnumerable<GameLoginData> GetProvidersData();

        UniTask<SignOutResult> SignOutAsync();
        
        UniTask<GameAuthResult> LoginAsync(string id,ILoginContext loginContext);
        
        UniTask<ResetCredentialResult> ResetCredentialAsync(string id,ILoginContext loginContext);
        
        UniTask<GameRegisterResult> RegisterAsync(string id,ILoginContext loginContext);

    }
}