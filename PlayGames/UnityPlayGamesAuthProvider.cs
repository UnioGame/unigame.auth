namespace UniGame.Runtime.GameAuth.PlayGames
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using FirebaseEmail;
    using UniCore.Runtime.ProfilerTools;
    using UnityEngine;

#if UNITY_ANDROID && PLAY_GAMES_ENABLED
    using GooglePlayGames;
    using GooglePlayGames.BasicApi;
#endif
    
    [Serializable]
    public class UnityPlayGamesAuthProvider: IGameAuthProvider
    {
        public int LoginTimeoutSeconds = 30;
        
        private readonly string _id;
        private AuthProviderResult _authResult = null;
        
        public UnityPlayGamesAuthProvider(string id)
        {
            _id = id;
        }
        
        public string Id => _id;
        
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

        public UniTask<SignOutResult> SignOutAsync()
        {
            return UniTask.FromResult(new SignOutResult(){success = true, error = string.Empty});
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

        public bool IsAuthSupported(IAuthContext context)
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
            //Настройка Play Games
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate(async x =>
            {
                Debug.Log($"PlayGamesPlatform Status : {x}");
                
                if (x != SignInStatus.Success)
                {
                    _authResult = new AuthProviderResult()
                    {
                        success = false,
                        error = x.ToString(),
                    };
                    
                    Debug.Log($"PlayGamesPlatform Login Failed");
                    return;
                }

                var token = string.Empty;
                var tokenCompleted = false;
                
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    token = code; // This token serves as an example to be used for SignInWithGooglePlayGames
                    tokenCompleted = true;
                });

                await UniTask.WaitWhile(() => tokenCompleted == false)
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
                    error = x.ToString(),
                };
            });

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