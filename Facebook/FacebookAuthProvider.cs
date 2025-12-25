namespace UniGame.Runtime.GameAuth.FacebookAuth
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Facebook.Unity;
    using UniCore.Runtime.ProfilerTools;

    [Serializable]
    public class FacebookAuthProvider : IGameAuthProvider
    {
        public const string FailedToInitialize = "Failed to initialize Facebook";
        public const string FailedToAuthenticate = "Failed to auth with Facebook";
        
        public List<string> loginPermissions = new(){"public_profile"};
        public int initializeWaitTimeout = 5000;
        public int initializeRetryDelay = 200;
        public int loginTimeout = 30000;
        
        private string _id;
        private bool _isInitialized = false;
        private ILoginResult _loginResult;
        private bool _waitForLogin = false;
        private bool _callbackCalled = false;
        private FacebookLoginResult authResult = new();

        public bool IsAuthenticated => FB.IsLoggedIn;

        public bool AllowRegisterAccount => false;

        public bool AllowRestoreAccount => false;


        public bool CheckAuthContext(IAuthContext context)
        {
            return context is FacebookAuthContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default)
        {
            var loginResult = new AuthProviderResult
            {
                success = false,
                data = null,
                error = string.Empty
            };
            
            _isInitialized = FB.IsInitialized;
            
            if (!_isInitialized)
                 await InitializeAsync(cancellationToken).Timeout<bool>(TimeSpan.FromSeconds(initializeWaitTimeout));
            
            if (!FB.IsInitialized)
            {
                loginResult.error = FailedToInitialize;
                return loginResult;
            }

            if (FB.IsLoggedIn == false)
            {
                _waitForLogin = true;
                FB.LogInWithReadPermissions (loginPermissions, OnLoginResult);
                await UniTask.WaitWhile(this,static x => x._waitForLogin,
                    cancellationToken:cancellationToken); 
            }
            
            if (FB.IsLoggedIn == false)
            {
                loginResult.error = FailedToAuthenticate;
                return loginResult;
            }
            
            var fbResult = UpdateAuthValue();

            await UniTask.SwitchToMainThread();

            return new AuthProviderResult()
            {
                success = fbResult.success,
                error = fbResult.error,
                data = new GameAuthData()
                {
                    userId = fbResult.userId,
                    token = fbResult.userToken,
                    displayName = string.Empty,
                    photoUrl = string.Empty,
                }
            };
        }

        public async UniTask<AuthSignOutResult> SignOutAsync()
        {
            FB.LogOut();
            
            return new AuthSignOutResult()
            {
                success = true,
                error = string.Empty,
            };
        }

        public void OnLoginResult(ILoginResult result)
        {
            _loginResult = result;
            _waitForLogin = false;
        }
        
        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            return AuthProviderResult.Failed;
        }

        private async UniTask<bool> InitializeAsync(CancellationToken cancellation = default)
        {
            while (cancellation.IsCancellationRequested == false &&
                   FB.IsInitialized == false)
            {
                _callbackCalled = false; 
            
                if (!FB.IsInitialized) {
                    // Initialize the Facebook SDK
                    FB.Init(InitCallback, OnHideUnity);
                } else {
                    _callbackCalled = true;
                }
                
                if(_callbackCalled == false)
                    await UniTask.WaitWhile(this, x => x._callbackCalled == false,cancellationToken:cancellation);

                if (FB.IsInitialized)
                {
                    GameLog.Log("Facebook SDK Initialized");
                    FB.ActivateApp();
                    return true;
                }
                
                await UniTask.Delay(TimeSpan.FromMilliseconds(initializeRetryDelay), cancellationToken: cancellation);
            }

            return FB.IsInitialized;
        }
        
        private void AuthCallback (ILoginResult result) 
        {
            UpdateAuthValue();
        }
        
        private void InitCallback ()
        {
            _callbackCalled = true;
        }

        private void OnHideUnity (bool isGameShown)
        {
            _callbackCalled = true;
        }
        
        private FacebookLoginResult UpdateAuthValue()
        {
            var isLoggedIn = FB.IsLoggedIn;
            authResult.complete = true;
            authResult.success = isLoggedIn;
            authResult.userToken =  AccessToken.CurrentAccessToken?.TokenString;
            authResult.token = AccessToken.CurrentAccessToken;
            authResult.userId = AccessToken.CurrentAccessToken?.UserId;
            authResult.error = isLoggedIn ? string.Empty : "failed to retrieve facebook access token";
            return authResult;
        }
    }
    
    public class FacebookLoginResult
    {
        public bool complete;
        public bool success;
        public string userToken;
        public string userId;
        public AccessToken token;
        public string error = string.Empty;
    }
}