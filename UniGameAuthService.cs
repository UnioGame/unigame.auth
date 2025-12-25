namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using R3;
    using UniCore.Runtime.ProfilerTools;
    using Rx;
    using UniGame.GameFlow.Runtime;
    using UnityEngine;

    [Serializable]
    public class UniGameAuthService : GameService, IUniGameAuthService
    {
        public const string GameAuthKey = nameof(GameAuthKey);
        
        private GameAuthConfiguration _configuration;
        private Subject<GameAuthResult> _authAction = new();
        private ReactiveValue<GameAuthResult> _authStatus = new();
        private ReactiveValue<GameAuthData> _authData = new();
        private Dictionary<string, GameLoginData> _loginData = new();
        private Dictionary<string, IGameAuthProvider> _providers = new();

        public UniGameAuthService(GameAuthConfiguration configuration)
        {
            _configuration = configuration;
            _authStatus.Value = new GameAuthResult()
            {
                success = false,
                error = string.Empty,
                id = string.Empty,
                data = null,
            };

            if (_configuration.userLoginCache)
            {
                RestoreAuthAsync().Forget();
            }
        }

        public Observable<GameAuthResult> AuthAction => _authAction;
        
        public ReadOnlyReactiveProperty<GameAuthResult> AuthStatus => _authStatus;

        public async UniTask<GameAuthResult> RestoreAuthAsync(CancellationToken ct = default)
        {
            var status = LoadAuthData();
            if(status == null || string.IsNullOrEmpty(status.id)) return status;
            
            var provider = GetProvider(status.id);
            var result = await provider.RestoreAsync(new EmptyAuthContext(),ct);

            var authResult = new GameAuthResult()
            {
                data = result.data,
                success = result.success,
                error = result.error,
                id = status.id,
            };
            
            SaveAuthData(authResult);

            _authStatus.Value = authResult;
            return authResult;
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

        public async UniTask<AuthSignOutResult> SignOutAsync()
        {
            var activeStatus = _authStatus.Value;
            if (!activeStatus.success)
            {
                return new AuthSignOutResult() { success = true, error = string.Empty };;
            }
            
            var provider = GetProvider(activeStatus.id);
            var result = await provider.SignOutAsync();
            if (result.success)
            {
                Reset();
            }
            
            return result;
        }

        public async UniTask<GameAuthResult> SignInAsync(IAuthContext loginContext, CancellationToken ct = default)
        {
            foreach (var authProvider in _providers)
            {
                var value = authProvider.Value;
                if(!value.CheckAuthContext(loginContext)) continue;
                return await SignInAsync(authProvider.Key, loginContext, ct);
            }

            return GameAuthResult.Failed;
        }

        public async UniTask<GameAuthResult> SignInAsync(string id, IAuthContext loginContext,CancellationToken ct = default)
        {
            var result = await LoginInternalAsync(id, loginContext, ct);
            
            if (result.success == false)
            {
                GameLog.LogError($"login to : {result.id} | {result.error}");
            }
            
            _authAction.OnNext(result);
            
            if (result.success)
                SetSignInResult(result);
            
            return result;
        }

        public async UniTask<ResetCredentialResult> ResetCredentialAsync(string id, IAuthContext loginContext)
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

        public async UniTask<GameRegisterResult> RegisterAsync(string id, IAuthContext loginContext,CancellationToken ct = default)
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
                var result = await gameAuthRegister.RegisterAsync(loginContext,ct);
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
        
        
        public IGameAuthProvider GetProvider(string id)
        {
            return _providers.GetValueOrDefault(id);
        }

        public GameLoginData GetAuthProviderData(string id)
        {
            return _loginData.GetValueOrDefault(id);
        }

        public IEnumerable<GameLoginData> GetAvailableAuth()
        {
            foreach (var data in _loginData)
            {
                var value = data.Value;
                if(!value.enabled) continue;
                yield return value;
            }
        }
        
        private async UniTask<GameAuthResult> LoginInternalAsync(string id, 
            IAuthContext loginContext, 
            CancellationToken cancellationToken = default)
        {
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

        
        private GameAuthResult LoadAuthData()
        {
            if (!PlayerPrefs.HasKey(GameAuthKey)) 
                return null;
            
            var json = PlayerPrefs.GetString(GameAuthKey);
            if (string.IsNullOrEmpty(json)) 
                return null;
            
            var status = JsonConvert.DeserializeObject<GameAuthResult>(json);
            return status;
        }
        
        private void SaveAuthData(GameAuthResult data)
        {
            var json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(GameAuthKey, json);
        }
    }

}