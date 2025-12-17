namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using R3;
    using Rx;
    using UniGame.GameFlow.Runtime;
    using UnityEngine.Localization;

    [Serializable]
    public class FakeAuthService : GameService,IGameAuthService
    {
        private string _userId;
        private GameAuthResult _authResult;
        private ReactiveValue<GameAuthResult> _authStatus = new();
        private GameLoginData _loginData;
        
        public FakeAuthService(string userId)
        {
            _userId = userId;
            _authResult = new GameAuthResult()
            {
                success = true,
                error = string.Empty,
                id = userId,
                data = new GameAuthData()
                {
                    displayName = userId,
                    userId = userId,
                }
            };
            
            _authStatus.Value = _authResult;

            _loginData = new GameLoginData()
            {
                authType = AuthType.Custom,
                authFactory = null,
                enabled = false,
                icon = null,
                providerName = "FakeAuth",
                description = new LocalizedString(),
                title = new LocalizedString()
            };
        }

        public Observable<GameAuthResult> AuthAction => _authStatus;
        
        public ReadOnlyReactiveProperty<GameAuthResult> AuthStatus => _authStatus;
        
        public void RegisterProvider(string id, GameLoginData data, IGameAuthProvider provider)
        {
        }

        public GameLoginData GetAuthProviderData(string id)
        {
            return _loginData;
        }

        public IEnumerable<GameLoginData> GetProvidersData()
        {
            yield break;
        }

        public async UniTask<SignOutResult> SignOutAsync()
        {
            return new SignOutResult()
            {
                error = string.Empty,
                success = true,
            };
        }

        public async UniTask<GameAuthResult> LoginAsync(string id, ILoginContext loginContext)
        {
            return _authResult;
        }

        public async UniTask<ResetCredentialResult> ResetCredentialAsync(string id, ILoginContext loginContext)
        {
            return new ResetCredentialResult()
            {
                error = string.Empty,
                success = false,
            };
        }

        public async UniTask<GameRegisterResult> RegisterAsync(string id, ILoginContext loginContext)
        {
            return new GameRegisterResult()
            {
                error = string.Empty,
                success = true,
                data = _authResult.data,
                id = _authResult.id,
            };
        }
    }
}