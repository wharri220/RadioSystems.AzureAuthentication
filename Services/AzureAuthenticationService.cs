﻿using System.Web;
using System.Web.Security;
using Orchard.Mvc;
using Orchard.Security;

namespace RadioSystems.AzureAuthentication.Services {
    public class AzureAuthenticationService : IAuthenticationService {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMembershipService _membershipService;

        public AzureAuthenticationService(IHttpContextAccessor httpContextAccessor, IMembershipService membershipService) {
            _httpContextAccessor = httpContextAccessor;
            _membershipService = membershipService;
        }

        public void SignIn(IUser user, bool createPersistentCookie) {}

        public void SignOut() {}

        public void SetAuthenticatedUserForRequest(IUser user) { }

        public IUser GetAuthenticatedUser() {
            var azureUser = _httpContextAccessor.Current().GetOwinContext().Authentication.User;

            if (!azureUser.Identity.IsAuthenticated) {
                return null;
            }

            var userName = azureUser.Identity.Name.Trim();

            var localUser = _membershipService.GetUser(userName) ?? 
                _membershipService.CreateUser(new CreateUserParams(userName, Membership.GeneratePassword(16, 5), userName, null, null, true));

            return localUser;
        }
    }
}