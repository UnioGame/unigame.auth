namespace UniGame.Runtime.GameAuth.DeviceIdGuest
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using FacebookAuth;
    using Firebase.Auth;
    using UniCore.Runtime.ProfilerTools;

    [Serializable]
    public class FirebaseAnonymousAuthProvider : IGameAuthProvider
    {
        private bool _isLoggedIn;
 
        public bool IsAuthenticated => _isLoggedIn;
        
        public bool AllowRestoreAccount => false;
        
        public bool AllowRegisterAccount => false;

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext authContext, CancellationToken cancellationToken = default)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var user = auth.CurrentUser;

            return new AuthProviderResult()
            {
                success = user != null && user.IsAnonymous == false,
                error = string.Empty,
                data = new GameAuthData()
                {
                    userId = user?.UserId,
                    email = user?.Email,
                    displayName = user?.DisplayName,
                }
            };
        }

        public UniTask<SignOutResult> SignOutAsync()
        {
            FirebaseAuth.DefaultInstance.SignOut();
            return UniTask.FromResult(new SignOutResult(){success = true, error = string.Empty});
        }
        
        public async UniTask<AuthProviderResult> RegisterAsync(IAuthContext context)
        {
            return await LoginAsync(context);
        }

        public bool IsAuthSupported(IAuthContext context)
        {
            return context is FirebaseAnonymousAuthContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default)
        {
            var result = new AuthProviderResult()
            {
                success = false,
                error = string.Empty,
            };
            
            var auth = FirebaseAuth.DefaultInstance;
            
            try
            {
                var authResult = await auth.SignInAnonymouslyAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                
                await UniTask.SwitchToMainThread();

                result.success = !string.IsNullOrEmpty(authResult.User.UserId);
                if (!result.success)
                {
                    result.error = "SignInAnonymouslyAsync failed";
                }
                else
                {
                    result.error = string.Empty;
                    result.data = new GameAuthData()
                    {
                        userId = authResult.User.UserId,
                        email = authResult.User.Email,
                        displayName = authResult.User.DisplayName,
                    };
                }
            }
            catch (Exception e)
            {
                GameLog.LogError(e);
                result.error = e.Message;
                result.success = false;
            }

            _isLoggedIn = result.success;
            
            return result;
        }
    }
}