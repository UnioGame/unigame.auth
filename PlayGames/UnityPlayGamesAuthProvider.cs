namespace UniGame.Runtime.GameAuth.PlayGames
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using FirebaseEmail;
    using UniCore.Runtime.ProfilerTools;
    using UnityEngine;
    using Utils;

#if UNITY_ANDROID && PLAY_GAMES_ENABLED
    using GooglePlayGames;
    using GooglePlayGames.BasicApi;
#endif
    
    [Serializable]
    public class UnityPlayGamesAuthProvider: IGameAuthProvider
    {
        public int LoginTimeoutSeconds = 30;
        
        private AuthProviderResult _authResult = null;
        private bool _tokenCompleted = false;
        private SignInStatus _signInStatus = SignInStatus.Canceled;
        
        public UnityPlayGamesAuthProvider()
        {
#if UNITY_ANDROID && PLAY_GAMES_ENABLED
            //Настройка Play Games
            PlayGamesPlatform.Activate();
#endif
        }

        public bool AllowRestoreAccount => true;

        public bool IsAuthenticated
        {
            get
            {
#if UNITY_ANDROID && PLAY_GAMES_ENABLED
                return PlayGamesPlatform.Instance.localUser.authenticated;
#endif
                return false;
            }
        }
        
        public bool AllowRegisterAccount => false;

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context,CancellationToken cancellationToken = default)
        {
            return await LoginAsync(context, cancellationToken);
        }

        public UniTask<AuthSignOutResult> SignOutAsync()
        {
            return UniTask.FromResult(new AuthSignOutResult(){success = true, error = string.Empty});
        }
        
        public UniTask<AuthProviderResult> RegisterAsync(IAuthContext context)
        {
            return new UniTask<AuthProviderResult>(new AuthProviderResult()
            {
                success = false,
                error = "PlayGamesPlatform does not support registration",
                data = null,
            });
        }

        public bool CheckAuthContext(IAuthContext context)
        {
            return context is UnityPlayGamesAuthContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await LoginByPlayServiceAsync(cancellationToken);
                return result;
            }
            catch (Exception e)
            {
                GameLog.LogError(e.Message);
                return new AuthProviderResult()
                {
                    data = null,
                    error = e.Message,
                    success = false,
                };
            }
        }
        
        public async UniTask<AuthProviderResult> LoginByPlayServiceAsync(CancellationToken cancellationToken = default)
        {
            _authResult = null;
            
#if !UNITY_ANDROID

            return new AuthProviderResult()
            {
                success = false,
                error = "Platform not supported",
            };
#endif
            
#if UNITY_ANDROID && PLAY_GAMES_ENABLED
            
            PlayGamesPlatform.Activate();
            var token = string.Empty;
            
            _signInStatus = SignInStatus.Canceled;
            _tokenCompleted = false;
            
            //Настройка Play Games
            PlayGamesPlatform.Instance.Authenticate(x =>
            {
                _signInStatus = x;
                
                Debug.Log($"PlayGamesPlatform Status : {x}");
                
                if (x != SignInStatus.Success)
                {
                    _authResult = new AuthProviderResult()
                    {
                        success = false,
                        error = x.ToStringFromCache(),
                    };
                    
                    Debug.Log($"PlayGamesPlatform Login Failed");
                    return;
                }
                
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    token = code; // This token serves as an example to be used for SignInWithGooglePlayGames
                    _tokenCompleted = true;
                });

            });
            
            await UniTask.WaitWhile(this,x => x._tokenCompleted == false,cancellationToken:cancellationToken)
                .Timeout(TimeSpan.FromSeconds(LoginTimeoutSeconds));
                
            var user = PlayGamesPlatform.Instance.localUser;
            var id = PlayGamesPlatform.Instance.GetUserId();
            var success = user is { authenticated: true };
                
            Debug.Log($"PlayGamesPlatform : {user?.userName} |  Auth: {success} | ID : {id}");

            _authResult = new AuthProviderResult()
            {
                success = success,
                data = new GameAuthData()
                {
                    userId = id,
                    displayName = PlayGamesPlatform.Instance.GetUserDisplayName(),
                    photoUrl = PlayGamesPlatform.Instance.GetUserImageUrl(),
                    email = string.Empty,
                    token = token,
                },
                error = _signInStatus.ToStringFromCache()
            };

            await UniTask.WaitWhile(this,
                    static x => x._authResult == null,cancellationToken:cancellationToken)
                .TimeoutWithoutException(TimeSpan.FromSeconds(LoginTimeoutSeconds));

            await UniTask.SwitchToMainThread();
            
            if (_authResult != null) return _authResult;
            
            Debug.Log($"PlayGamesPlatform : Login Timeout");
            
            return new AuthProviderResult()
            {
                success = false,
                error = "PlayGamesPlatform Login Timeout",
            };
#endif

            return new AuthProviderResult()
            {
                success = false,
                error = "Platform not supported",
            };
        }
        
    }

}