namespace PlayGames.NakamaGoogle
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using UniGame.MetaBackend.Runtime;
    using UniGame.Runtime.GameAuth;
    using UniGame.Runtime.GameAuth.PlayGames;

    public class NakamaPlayServicesAuthProvider : IGameAuthProvider
    {
        private bool _isAuthenticated;
        
        private PlayGamesAuthProvider _playGamesAuthProvider;
        private NakamaPlayServicesAuthContract _nakamaGoogleAuthContract;
        private NakamaLogoutContract _nakamaLogoutContract;
        private NakamaRestoreSessionContract _nakamaRestoreSessionContract;

        public NakamaPlayServicesAuthProvider(string id)
        {
            _playGamesAuthProvider = new PlayGamesAuthProvider();
            _nakamaGoogleAuthContract = new NakamaPlayServicesAuthContract();
            _nakamaLogoutContract = new NakamaLogoutContract();
            _nakamaRestoreSessionContract = new NakamaRestoreSessionContract();
        }

        public bool IsAuthenticated => _isAuthenticated;

        public bool AllowRegisterAccount => true;

        public bool AllowRestoreAccount => false;

        public bool CheckAuthContext(IAuthContext context)
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
            return ToProviderResult(contractResult.data);
        }

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            var restoreResult = await _nakamaRestoreSessionContract.ExecuteAsync(cancellationToken);

            return ToProviderResult(restoreResult.data);
        }

        public async UniTask<AuthSignOutResult> SignOutAsync()
        {
            var result = await _nakamaLogoutContract.ExecuteAsync();
            return new AuthSignOutResult()
            {
                success = result.success,
            };
        }
        
        private AuthProviderResult ToProviderResult(NakamaAuthResult response)
        {
            var success = response is { success: true };
            if (!success) return AuthProviderResult.Failed;
            
            var result = new AuthProviderResult()
            {
                success = true,
                error = response.error,
            };
            
            if(!response.success) return AuthProviderResult.Failed;

            result.data = new GameAuthData()
            {
                userId = response.account.User.Id,
                displayName = response.account.User.DisplayName,
                token = response.account.User.Id,
                photoUrl = response.account.User.AvatarUrl,
                email = response.account.Email,
            };

            _isAuthenticated = true;

            return result;
        }
    }
}