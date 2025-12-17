namespace UniGame.Runtime.GameAuth.FacebookAuth
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Facebook.Unity;
    using Firebase.Auth;
    using UniCore.Runtime.ProfilerTools;

    [Serializable]
    public class FirebaseFacebookAuthProvider : IGameAuthProvider
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

        public FirebaseFacebookAuthProvider(string id)
        {
            _id = id;
        }

        public bool IsAuthenticated => FB.IsLoggedIn;

        public bool AllowRegisterAccount => false;

        public bool AllowRestoreAccount => false;
        

        public async UniTask<AuthProviderResult> LoginAsync(ILoginContext context,CancellationToken cancellationToken = default)
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
            
            var data = UpdateAuthValue();
            var result = await SignInToFirebaseAsync(data.userToken,cancellationToken);
            
            await UniTask.SwitchToMainThread();
            
            return result;
        }

        public async UniTask<SignOutResult> SignOutAsync()
        {
            FB.LogOut();
            
            return new SignOutResult()
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

        private async UniTask<AuthProviderResult> SignInToFirebaseAsync(string accessToken,CancellationToken token = default)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var credential = Firebase.Auth.FacebookAuthProvider.GetCredential(accessToken);
            var task = auth.SignInAndRetrieveDataWithCredentialAsync(credential);
            var signinResult = await task.AsUniTask()
                    .AttachExternalCancellation(token);
            
            await UniTask.SwitchToMainThread();

            var user = signinResult.User;
            var isAuthenticated = user != null && !string.IsNullOrEmpty(user.UserId);
            
            if (task.IsCanceled)
            {
                isAuthenticated = false;
            }
            if (task.IsFaulted)
            {
                isAuthenticated = false;
            }

            var result = new AuthProviderResult()
            {
                success = isAuthenticated,
                data = new GameAuthData()
                {
                    userId = user?.UserId,
                    displayName = user?.DisplayName,
                    email = user?.Email,
                    photoUrl = user?.PhotoUrl?.ToString(),
                    token = accessToken,
                },
                error = isAuthenticated ? string.Empty 
                    : $"failed to sign in to Firebase with Facebook token: {task.Exception?.Message}",
            };

            return result;
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
            authResult.userToken = isLoggedIn ? AccessToken.CurrentAccessToken.TokenString : string.Empty;
            authResult.token = isLoggedIn? AccessToken.CurrentAccessToken : null;
            authResult.userId = isLoggedIn ? AccessToken.CurrentAccessToken.UserId : string.Empty;
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