namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using R3;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Runtime.Rx;
    using UniGame.GameFlow.Runtime;
    using UnityEngine;

    [Serializable]
    public class GameAuthService : GameService, IGameAuthService
    {
        public const string GameAuthKey = nameof(GameAuthKey);
        
        private GameAuthConfiguration _configuration;
        private Subject<GameAuthResult> _authAction = new();
        private ReactiveValue<GameAuthResult> _authStatus = new();
        private ReactiveValue<GameAuthData> _authData = new();
        private Dictionary<string, GameLoginData> _loginData = new();
        private Dictionary<string, IGameAuthProvider> _providers = new();

        public GameAuthService(GameAuthConfiguration configuration)
        {
            _configuration = configuration;
            _authStatus.Value = new GameAuthResult()
            {
                success = false,
                error = string.Empty,
                id = string.Empty,
                data = null,
            };
            
            if (_configuration.debugMode)
            {
                var userId = _configuration.overrideUserId
                    ? _configuration.targetUserId
                    : SystemInfo.deviceUniqueIdentifier;

                _authStatus.Value = new GameAuthResult()
                {
                    success = true,
                    error = string.Empty,
                    data = new GameAuthData()
                    {
                        displayName = userId,
                        userId = userId,
                    }
                };
            }

            if (_configuration.userLoginCache)
                RestoreAuth();
        }

        public Observable<GameAuthResult> AuthAction => _authAction;
        
        public ReadOnlyReactiveProperty<GameAuthResult> AuthStatus => _authStatus;

        public void RestoreAuth()
        {
            if (!PlayerPrefs.HasKey(GameAuthKey)) return;
            var json = PlayerPrefs.GetString(GameAuthKey);
            if (string.IsNullOrEmpty(json)) return;
            
            var value = JsonConvert.DeserializeObject<GameAuthData>(json);
            if (value == null) return;

            _authData.Value = value;
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(GameAuthKey);
            _authStatus.Value = new GameAuthResult()
            {
                success = false,
                error = string.Empty,
                id = string.Empty,
                data = null,
            };
        }

        public void RegisterProvider(string id, GameLoginData data, IGameAuthProvider provider)
        {
            _loginData[id] = data;
            _providers[id] = provider;
        }

        public async UniTask<SignOutResult> SignOutAsync()
        {
            var activeStatus = _authStatus.Value;
            if (!activeStatus.success)
            {
                return new SignOutResult() { success = true, error = string.Empty };;
            }
            
            var provider = GetProvider(activeStatus.id);
            var result = await provider.SignOutAsync();
            if (result.success)
            {
                Reset();
            }
            
            return result;
        }

        public async UniTask<GameAuthResult> LoginAsync(string id, ILoginContext loginContext)
        {
            var result = await LoginInternalAsync(id, loginContext);
            
            if (result.success == false)
            {
                GameLog.LogError($"login to : {result.id} | {result.error}");
            }
            
            _authAction.OnNext(result);
            
            if (result.success)
                SetSignInResult(result);
            
            return result;
        }

        public async UniTask<ResetCredentialResult> ResetCredentialAsync(string id, ILoginContext loginContext)
        {
            var provider = GetProvider(id);
            if (provider is not IGameAuthReset resetProvider)
            {
                return new ResetCredentialResult()
                {
                    success = false,
                    error = string.Empty,
                };
            }

            try
            {
                var result = await resetProvider.ResetAuthAsync(loginContext);
                return result;
            }
            catch (Exception e)
            {
                GameLog.Log(e.Message, Color.red);
                return new ResetCredentialResult()
                {
                    success = false,
                    error = e.Message,
                };
            }
        }

        public async UniTask<GameRegisterResult> RegisterAsync(string id, ILoginContext loginContext)
        {
            var provider = GetProvider(id);
            if (provider is not IGameAuthRegister gameAuthRegister)
            {
                return new GameRegisterResult()
                {
                    id = id,
                    success = false,
                    error = "No provider found",
                    data = null,
                };
            }

            var registerResult = new GameRegisterResult()
            {
                id = id,
                success = false,
                error = string.Empty,
                data = null,
            };
            
            try
            {
                var result = await gameAuthRegister.RegisterAsync(loginContext);
                registerResult.success = result.success;
                registerResult.id = id;
                registerResult.error = result.error;
                registerResult.data = result.data;
            }
            catch (Exception e)
            {
                GameLog.LogError(e.Message);
                registerResult.error = e.Message;
                registerResult.success = false;
            }
            
            return registerResult;
        }

        public void SetSignInResult(GameAuthResult result)
        {
            if (result == null) return;
            
            _authStatus.Value = result;
            var authData = result.data;

            if (!result.success || authData == null) return;
            
            var json = JsonConvert.SerializeObject(authData);
            PlayerPrefs.SetString(GameAuthKey, json);

        }
        
        private async UniTask<GameAuthResult> LoginInternalAsync(string id
            , ILoginContext loginContext
            ,CancellationToken cancellationToken = default)
        {
            var activeStatus = _authStatus.Value;
            
            //already logged in
            if (activeStatus.success)
            {
                if (id.Equals(activeStatus.id)) return activeStatus;
                return new GameAuthResult()
                {
                    success = false,
                    id = id,
                    error = "Already logged in",
                    data = activeStatus.data,
                };
            }
            
            var provider = GetProvider(id);
            var result = new GameAuthResult();
            result.id = id;
            
            if (provider == null)
            {
                result.id = id;
                result.error = "No provider found";
                result.success = false;
                result.data = null;
                return result;
            }
            try
            {
                GameLog.Log($"Start login to: {id} | {provider}");
                
                var loginTaskResult = await provider
                    .LoginAsync(loginContext,cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(_configuration.authTimeout));
                
                result.success = loginTaskResult.success;
                result.id = id;
                result.error = loginTaskResult?.error;
                result.data = loginTaskResult?.data;
            }
            catch (Exception e)
            {
                GameLog.LogError(e);
                result.error = e.Message;
                result.success = false;
            }

            return result;
        }

        public IGameAuthProvider GetProvider(string id)
        {
            return _providers.GetValueOrDefault(id);
        }

        public GameLoginData GetAuthProviderData(string id)
        {
            return _loginData.GetValueOrDefault(id);
        }

        public IEnumerable<GameLoginData> GetProvidersData()
        {
            return _loginData.Values;
        }
    }
}