namespace UniGame.Runtime.GameAuth.PlayGames
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniCore.Runtime.ProfilerTools;
    using Utils;

#if UNITY_ANDROID && PLAY_GAMES_ENABLED
    using GooglePlayGames;
    using GooglePlayGames.BasicApi;
    using UnityEngine;
#endif
    
    [Serializable]
    public class PlayGamesAuthProvider: IGameAuthProvider
    {
        public int LoginTimeoutSeconds = 30;
        
        private AuthProviderResult _authResult = null;
        private bool _tokenCompleted = false;
        private bool _isActivated = false;
        private string _token = string.Empty;

#if UNITY_ANDROID && PLAY_GAMES_ENABLED
        private SignInStatus _signInStatus = SignInStatus.Canceled;
#endif

        public PlayGamesAuthProvider()
        {
// #if UNITY_ANDROID && PLAY_GAMES_ENABLED
//             //Настройка Play Games
//             PlayGamesPlatform.Activate();
// #endif
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
            return context is PlayGamesAuthContext;
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

        
        public void Activate()
        {
#if UNITY_ANDROID && PLAY_GAMES_ENABLED

            if (_isActivated) return;

            PlayGamesPlatform.Activate();
            
            _isActivated = true;
#endif
        }

#if UNITY_ANDROID && PLAY_GAMES_ENABLED
        
        public void ApplyAuthStatus(SignInStatus x)
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
                
                _tokenCompleted = true;
                return;
            }
                
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
            {
                Debug.Log("Authorization code: " + code);
                _token = code; // This token serves as an example to be used for SignInWithGooglePlayGames
                _tokenCompleted = true;
            });

        }
#endif
        
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

            Activate();
            
            _token = string.Empty;
            _signInStatus = SignInStatus.Canceled;
            _tokenCompleted = false;
            
            PlayGamesPlatform.Instance.Authenticate(ApplyAuthStatus);
            
            await UniTask.WaitWhile(this,x => x._tokenCompleted == false,cancellationToken:cancellationToken)
                .Timeout(TimeSpan.FromSeconds(LoginTimeoutSeconds));
                
            var user = PlayGamesPlatform.Instance.localUser;
            var success = _signInStatus == SignInStatus.Success;
                
            Debug.Log($"PlayGamesPlatform : {user?.userName} |  Auth: {success} | ID : {user?.id}");

            _authResult = new AuthProviderResult()
            {
                success = success,
                data = new GameAuthData()
                {
                    userId = PlayGamesPlatform.Instance.GetUserId(),
                    displayName = PlayGamesPlatform.Instance.GetUserDisplayName(),
                    photoUrl = PlayGamesPlatform.Instance.GetUserImageUrl(),
                    email = string.Empty,
                    token = _token,
                },
                error = _signInStatus.ToStringFromCache()
            };
            
            await UniTask.SwitchToMainThread();
            
            return _authResult;
#endif

            return new AuthProviderResult()
            {
                success = false,
                error = "Platform not supported",
            };
        }
        
    }

}