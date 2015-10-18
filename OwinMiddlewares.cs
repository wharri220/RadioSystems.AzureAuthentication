﻿using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Web.Helpers;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.ActiveDirectory;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.OpenIdConnect;
using Orchard.ContentManagement;
using Orchard.Logging;
using Orchard.Owin;
using Orchard.Settings;
using Owin;
using RadioSystems.AzureAuthentication.Constants;
using RadioSystems.AzureAuthentication.Models;
using RadioSystems.AzureAuthentication.Security;

namespace RadioSystems.AzureAuthentication {
    public class OwinMiddlewares : IOwinMiddlewareProvider {
        public ILogger Logger { get; set; }
       
        private readonly string _azureClientId;
        private readonly string _azureTenant;
        private readonly string _azureADInstance;
        private readonly string _logoutRedirectUri;
        private readonly string _azureAppName;
        private readonly bool _sslEnabled;
        private readonly bool _azureWebSiteProtectionEnabled;

        public OwinMiddlewares(ISiteService siteService) {
            Logger = NullLogger.Instance;

            var site = siteService.GetSiteSettings();
            var azureSettings = site.As<AzureSettingsPart>();

            _azureClientId = azureSettings.ClientId ?? "5af0d675-5736-4e38-bd94-493118e422cf";
            _azureTenant = azureSettings.Tenant ?? "invisiblefence.com";
            _azureADInstance = "https://login.microsoft.com/{0}";
            _logoutRedirectUri = site.BaseUrl;
            _azureAppName = azureSettings.AppName ?? "AuthModuleDemo";
            _sslEnabled = azureSettings.SSLEnabled;
            _azureWebSiteProtectionEnabled = azureSettings.AzureWebSiteProtectionEnabled;
        }

        public IEnumerable<OwinMiddlewareRegistration> GetOwinMiddlewares() {
            var middlewares = new List<OwinMiddlewareRegistration>();

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            var openIdOptions = new OpenIdConnectAuthenticationOptions {
                ClientId = _azureClientId,
                Authority = string.Format(CultureInfo.InvariantCulture, _azureADInstance, _azureTenant),
                PostLogoutRedirectUri = _logoutRedirectUri,
                Notifications = new OpenIdConnectAuthenticationNotifications ()
            };

            var cookieOptions = new CookieAuthenticationOptions();

            var bearerAuthOptions = new WindowsAzureActiveDirectoryBearerAuthenticationOptions {
                TokenValidationParameters = new TokenValidationParameters {
                    ValidAudience = string.Format(_sslEnabled ? "https://{0}/{1}" : "http://{0}/{1}", _azureTenant, _azureAppName)
                }
            };

            if (_azureWebSiteProtectionEnabled) {
                middlewares.Add(new OwinMiddlewareRegistration {
                    Priority = "9",
                    Configure = app => { app.SetDataProtectionProvider(new MachineKeyProtectionProvider()); }
                });
            }

            middlewares.Add(new OwinMiddlewareRegistration {
                Priority = "10",
                Configure = app => {
                    app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

                    app.UseCookieAuthentication(cookieOptions);

                    app.UseOpenIdConnectAuthentication(openIdOptions);

                    //This is throwing an XML DTD is prohibited error?
                    //app.UseWindowsAzureActiveDirectoryBearerAuthentication(bearerAuthOptions);
                }
            });

            return middlewares;
        }
    }
}