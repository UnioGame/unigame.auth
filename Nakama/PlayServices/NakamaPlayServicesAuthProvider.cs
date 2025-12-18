namespace PlayGames.NakamaGoogle
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using UniGame.Runtime.GameAuth;
    using UniGame.Runtime.GameAuth.PlayGames;

    public class NakamaPlayServicesAuthProvider : IGameAuthProvider
    {
        private readonly string _id;
        
        private bool _isAuthenticated;
        
        private UnityPlayGamesAuthProvider _playGamesAuthProvider;
        private NakamaGoogleAuthContract _nakamaGoogleAuthContract;
        private NakamaLogoutContract _nakamaLogoutContract;
        private NakamaRestoreSessionContract _nakamaRestoreSessionContract;

        public NakamaPlayServicesAuthProvider(string id)
        {
            _id = id;
            _playGamesAuthProvider = new UnityPlayGamesAuthProvider(id);
            _nakamaGoogleAuthContract = new NakamaGoogleAuthContract();
            _nakamaLogoutContract = new NakamaLogoutContract();
            _nakamaRestoreSessionContract = new NakamaRestoreSessionContract();
        }

        public bool IsAuthenticated => _isAuthenticated;

        public bool AllowRegisterAccount => true;

        public bool AllowRestoreAccount => false;

        public bool IsAuthSupported(IAuthContext context)
        {
            return context is NakamaGoogleAuthContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            var linkAccount = false;
            var createAccount = true;
            
            if (context is NakamaGoogleAuthContext nakamaContext)
            {
                linkAccount = nakamaContext.linkAccount;
                createAccount = nakamaContext.createAccount;
            }
            
            var googleAuthResult = await _playGamesAuthProvider.LoginAsync(context, cancellationToken);
            if (googleAuthResult.success == false)
                return googleAuthResult;

            var googleData = googleAuthResult.data;
            
            var data = _nakamaGoogleAuthContract.data;
            data.token = googleData.token;
            data.userName = googleData.userId;
            data.linkAccount = linkAccount;
            data.create = createAccount;
            
            var contractResult = await _nakamaGoogleAuthContract.ExecuteAsync(cancellationToken);
            GameAuthData authData = null;

            if (contractResult.success)
            {
                var nakamaData = contractResult.data;

                authData = new GameAuthData()
                {
                    userId = nakamaData.account?.User?.Id,
                    token = data.token,
                    displayName = googleData.displayName,
                    email = googleData.email,
                    photoUrl = googleData.photoUrl,
                };
            }

            var result = new AuthProviderResult()
            {
                success = contractResult.success,
                error = contractResult.error,
                data = authData,
            };
            
            return result;
        }

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            var restoreResult = await _nakamaRestoreSessionContract.ExecuteAsync(cancellationToken);
            if (restoreResult.success == false)
                return AuthProviderResult.Failed;
            
            var data = restoreResult.data;
            if(data.success == false)
                return AuthProviderResult.Failed;
            
            GameAuthData authData = null;
            
            if (data != null)
            {
                authData = new GameAuthData()
                {
                    userId = data.account?.User.Id,
                    token = data.account?.User.Id,
                    displayName = data.account?.User?.DisplayName,
                    photoUrl = data.account?.User.AvatarUrl,
                    email = data.account?.Email,
                };
            }
            
            return new AuthProviderResult()
            {
                success = restoreResult.success,
                error = restoreResult.error,
                data = authData,
            };
        }

        public async UniTask<SignOutResult> SignOutAsync()
        {
            var result = await _nakamaLogoutContract.ExecuteAsync();
            return new SignOutResult()
            {
                success = result.success,
            };
        }
    }
}