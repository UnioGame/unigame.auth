namespace UniGame.Runtime.GameAuth.PlayGames
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniCore.Runtime.ProfilerTools;
    using UnityEngine;
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
                var interactiveAllowed = context is not PlayGamesAuthContext playGamesContext ||
                    playGamesContext.interactiveAllowed;
                var result = await LoginByPlayServiceAsync(interactiveAllowed, cancellationToken);
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
            return await LoginByPlayServiceAsync(true, cancellationToken);
        }

        public async UniTask<AuthProviderResult> LoginByPlayServiceAsync(
            bool interactiveAllowed,
            CancellationToken cancellationToken = default)
        {
#if !UNITY_ANDROID
            return new AuthProviderResult()
            {
                success = false,
                error = "Platform not supported",
            };
#endif
            
            _authResult = null;
            
            GameLog.Log($"[PlayGamesAuth] Start auth Play Service", Color.yellow);
            
#if PLAY_GAMES_ENABLED
            Activate();

            _authResult = new AuthProviderResult()
            {
                success = false
            };
            
            await UniTask.SwitchToMainThread();

            var signInStatus = new UniTaskCompletionSource<SignInStatus>();
            PlayGamesPlatform.Instance.Authenticate(status => signInStatus.TrySetResult(status));
            
            var signInResult = await signInStatus.Task.AttachExternalCancellation(cancellationToken);

            if (signInResult != SignInStatus.Success && interactiveAllowed)
            {
                GameLog.Log($"[PlayGamesAuth] Authentication Failed. Try to do it manually.", Color.yellow);
                var manualTcs = new UniTaskCompletionSource<SignInStatus>();
                PlayGamesPlatform.Instance.ManuallyAuthenticate(status => manualTcs.TrySetResult(status));
                signInResult = await manualTcs.Task.AttachExternalCancellation(cancellationToken);
            }
            
            if (signInResult != SignInStatus.Success)
            {
                GameLog.Log($"[PlayGamesAuth] Manually authentication Failed.", Color.yellow);
                var messageError = $"Error code: {signInResult}.";
                _authResult.error = messageError;
                return _authResult;
            }

            GameLog.Log($"[PlayGamesAuth] Authentication done.", Color.green);
            var authCode = new UniTaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, x => authCode.TrySetResult(x));
            
            var authCodeResult = await authCode.Task.AttachExternalCancellation(cancellationToken);

            if (string.IsNullOrEmpty(authCodeResult))
            {
                var messageError = "Empty server side auth code";
                Debug.LogError($"[PlayGamesAuth] {messageError}");
                _authResult.error = messageError;
                return _authResult;
            }

            _authResult.success = true;
            _authResult.data.userId = PlayGamesPlatform.Instance.GetUserId();
            _authResult.data.displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            _authResult.data.photoUrl = PlayGamesPlatform.Instance.GetUserImageUrl();
            _authResult.data.token = authCodeResult;

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
