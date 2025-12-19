namespace UniGame.Runtime.GameAuth.FirebaseEmail
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Firebase.Auth;
    using R3;
    using Rx;
    using UnityEngine;

    [Serializable]
    public class FirebaseEmailAuthProvider : IGameAuthProvider,IGameAuthReset,IGameAuthRegister
    {
        private string _id;
        private ReactiveValue<bool> _isLoggedIn = new();
        private ReactiveValue<AuthProviderResult> _authResult = new();

        public FirebaseEmailAuthProvider(string id)
        {
            _id = id;
        }

        public ReadOnlyReactiveProperty<AuthProviderResult> AuthResult => _authResult;
        
        public string Id => _id;
        
        public bool AllowRestoreAccount => true;
        
        public bool IsAuthenticated => _isLoggedIn.Value;
        
        public bool AllowRegisterAccount => true;

        public async UniTask<AuthProviderResult> RegisterAsync(IAuthContext context)
        {
            var result = new AuthProviderResult()
            {
                success = false,
                error = "Invalid context",
                data = null,
            };

            if (context is not EmailAuthContext emailContext) return result;
            
            var registered = await RegisterUser(emailContext.Email, emailContext.Password);
            return registered;
        }

        public bool CheckAuthContext(IAuthContext context)
        {
            return context is EmailAuthContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default)
        {
            var result = new AuthProviderResult()
            {
                success = false,
                error = "Invalid context",
                data = null,
            };

            var emailContext = context as EmailAuthContext;
            if (emailContext == null) return result;

            var app = FirebaseAuth.DefaultInstance;
            var task = app.SignInWithEmailAndPasswordAsync(emailContext.Email, emailContext.Password)
                .AsUniTask()
                .AttachExternalCancellation(cancellationToken);

            var taskResult = await task;
            await UniTask.SwitchToMainThread();
            
            if (task.Status != UniTaskStatus.Succeeded)
            {
                var error = $"Email login error: {emailContext.Email} STATUS {task.Status}";
                Debug.Log(error);
                result.error = error;
                return result;
            }

            var authResult = taskResult;
            var user = authResult.User;
            
            Debug.Log($"User SignIn by EMAIL: {user.DisplayName} {user.UserId} {user.Email}");

            //if (user.IsEmailVerified == false)
            //    SendVerificationEmail(user).Forget();
            
            _isLoggedIn.Value = true;
            
            return new AuthProviderResult()
            {
                data = new GameAuthData()
                {
                    displayName = user.DisplayName,
                    email = user.Email,
                    userId = user.UserId,
                },
                error = string.Empty,
                success = user.IsAnonymous == false,
            };
        }

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, 
            CancellationToken cancellationToken = default)
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
            var app = FirebaseAuth.DefaultInstance;
            app.SignOut();
            return UniTask.FromResult(new SignOutResult(){success = true, error = string.Empty});
        }

        public async UniTask<AuthProviderResult> RegisterUser(string emailData, string passwordData)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var errorMessage = string.Empty;
            var task = auth.CreateUserWithEmailAndPasswordAsync(emailData, passwordData)
                .AsUniTask()
                .SuppressCancellationThrow();

            try
            {
                var result = await task;
                
                await UniTask.SwitchToMainThread();

                if (task.Status != UniTaskStatus.Succeeded)
                {
                    var error = $"Email registration filed: {emailData} STATUS {task.Status}";
                    Debug.Log(error);
                    return new AuthProviderResult()
                    {
                        data = null,
                        error = error,
                        success = false,
                    };
                }
            
                var authResult = result.Result;
                var user = authResult.User;
                
                if (!user.IsEmailVerified)
                    SendVerificationEmail(user).Forget();
            
                Debug.LogFormat($"Firebase Email registration complete: {user.DisplayName} { user.UserId}");
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                Debug.Log(e.Message);
            }
            
            return new AuthProviderResult()
            {
                success = auth.CurrentUser.IsAnonymous == false,
                error = errorMessage,
                data = new GameAuthData()
                {
                    userId = auth.CurrentUser.UserId,
                    email = auth.CurrentUser.Email,
                    displayName = auth.CurrentUser.DisplayName,
                }
            };
        }

        public async UniTask<ResetCredentialResult> ResetAuthAsync(IAuthContext context)
        {
            
            if (context is not EmailAuthContext emailContext)
            {
                return new ResetCredentialResult()
                {
                    success = false,
                    error = "Invalid context",
                };
            };

            var auth = FirebaseAuth.DefaultInstance;
            
            try
            {
                var task = auth.SendPasswordResetEmailAsync(emailContext.Email).AsUniTask();
                await task.SuppressCancellationThrow();
                await UniTask.SwitchToMainThread();
                return new ResetCredentialResult()
                {
                    success = task.Status == UniTaskStatus.Succeeded,
                    error = string.Empty,
                };
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return new ResetCredentialResult()
                {
                    success = false,
                    error = e.Message,
                };
            }
        }
        
        public async UniTask<bool> SendVerificationEmail(FirebaseUser user)
        {
            var verificationTask = user
                .SendEmailVerificationAsync()
                .AsUniTask();
            
            await verificationTask.SuppressCancellationThrow();
            await UniTask.SwitchToMainThread();

            if (verificationTask.Status == UniTaskStatus.Succeeded)
            {
                Debug.Log("Firebase Verification email sent!");
            }
            else
            {
                Debug.Log("Firebase Failed to send verification email: " + verificationTask.Status);
            }
            return verificationTask.Status == UniTaskStatus.Succeeded;
        }

        public UniTask<AuthProviderResult> RegisterAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}