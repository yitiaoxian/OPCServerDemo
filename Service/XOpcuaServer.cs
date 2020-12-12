using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Service
{
    class XOpcuaServer : StandardServer
    {
        #region 重载方法
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Utils.Trace("Creating the node managers");
            List<INodeManager> nodeManagers = new List<INodeManager>();
            nodeManagers.Add(new OpcuaNodeManager(server,configuration));
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
        #endregion
        /// <summary>
        /// 为应用加载非配置的属性
        /// </summary>
        /// 这些属性服务器可见，用户不可更改
        /// <returns></returns>
        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();
            properties.ManufacturerName = "OPC Foundation";
            properties.ProductName = "Quickstart Reference Server";
            properties.ProductUri = "http://opcfoundation.org/Quickstart/ReferenceServer/v1.04";
            properties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber = Utils.GetAssemblyBuildNumber();
            properties.BuildDate = Utils.GetAssemblyTimestamp();

            return properties;
        }
        protected override ResourceManager CreateResourceManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            ResourceManager resourceManager = new ResourceManager(server,configuration);
            System.Reflection.FieldInfo[] fieldInfos = typeof(StatusCode).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (System.Reflection.FieldInfo field in fieldInfos)
            {
                //uint?表示可空类型
                uint? id = field.GetValue(typeof(StatusCode)) as uint?;
                if (id != null)
                {
                    resourceManager.Add(id.Value,"en-US",field.Name);
                }
            }
            return resourceManager;
        }

        protected override void OnServerStarting(ApplicationConfiguration configuration)
        {
            Utils.Trace("The server is starting.");
            base.OnServerStarting(configuration);
            CreateUserIdentityValidators(configuration);
        }
        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);
            server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);
            try
            {
                server.Status.Variable.CurrentTime.MinimumSamplingInterval = 250;
            }
            catch { }
        }

        private void CreateUserIdentityValidators(ApplicationConfiguration configuration)
        {
            for(int ii=0;ii < configuration.ServerConfiguration.UserTokenPolicies.Count; ii++)
            {
                UserTokenPolicy policy = configuration.ServerConfiguration.UserTokenPolicies[ii];

                if(policy.TokenType == UserTokenType.Certificate)
                {
                    if(configuration.SecurityConfiguration.TrustedUserCertificates != null &&
                        configuration.SecurityConfiguration.UserIssuerCertificates != null)
                    {
                        CertificateValidator certificateValidator = new CertificateValidator();
                        certificateValidator.Update(configuration.SecurityConfiguration).Wait();
                        certificateValidator.Update(configuration.SecurityConfiguration.UserIssuerCertificates,
                            configuration.SecurityConfiguration.TrustedUserCertificates,
                            configuration.SecurityConfiguration.RejectedCertificateStore);

                        m_userCertificateValidator = certificateValidator.GetChannelValidator();
                    }
                }
            }
        }

        private void SessionManager_ImpersonateUser(Session session,ImpersonateEventArgs args)
        {
            UserNameIdentityToken userNameIdentityToken = args.NewIdentity as UserNameIdentityToken;
            if(userNameIdentityToken != null)
            {
                args.Identity = VerifyPassword(userNameIdentityToken);
                args.Identity.GrantedRoleIds.Add(ObjectIds.WellKnownRole_AuthenticatedUser);

                if(args.Identity is SystemConfigurationIdentity)
                {
                    args.Identity.GrantedRoleIds.Add(ObjectIds.WellKnownRole_ConfigureAdmin);
                    args.Identity.GrantedRoleIds.Add(ObjectIds.WellKnownRole_SecurityAdmin);
                }
                return;
            }
            X509IdentityToken x509IdentityToken = args.NewIdentity as X509IdentityToken;
            if (x509IdentityToken != null)
            {
                VerifyUserTokenCertificate(x509IdentityToken.Certificate);
                args.Identity = new UserIdentity(x509IdentityToken);
                Utils.Trace("X509 Token Accepted: {0}", args.Identity.DisplayName);
                args.Identity.GrantedRoleIds.Add(ObjectIds.WellKnownRole_AuthenticatedUser);
                return;
            }
            args.Identity = new UserIdentity();
            args.Identity.GrantedRoleIds.Add(ObjectIds.WellKnownRole_Anonymous);
        }
        private void VerifyUserTokenCertificate(X509Certificate2 certificate)
        {
            try
            {
                if (m_userCertificateValidator != null)
                {
                    m_userCertificateValidator.Validate(certificate);
                }
                else
                {
                    CertificateValidator.Validate(certificate);
                }
            }
            catch (Exception e)
            {
                TranslationInfo info;
                StatusCode result = StatusCodes.BadIdentityTokenRejected;
                ServiceResultException se = e as ServiceResultException;
                if (se != null && se.StatusCode == StatusCodes.BadCertificateUseNotAllowed)
                {
                    info = new TranslationInfo(
                        "InvalidCertificate",
                        "en-US",
                        "'{0}' is an invalid user certificate.",
                        certificate.Subject);

                    result = StatusCodes.BadIdentityTokenInvalid;
                }
                else
                {
                    info = new TranslationInfo(
                        "UntrustedCertificate",
                        "en-US",
                        "'{0}' is not a trusted user certificate.",
                        certificate.Subject);
                }

                throw new ServiceResultException(new ServiceResult(
                    result,
                    info.Key,
                    LoadServerProperties().ProductUri,
                    new LocalizedText(info)));
            }
        }

        private IUserIdentity VerifyPassword(UserNameIdentityToken userNameIdentityToken)
        {
            var userName = userNameIdentityToken.UserName;
            var password = userNameIdentityToken.DecryptedPassword;
            if (String.IsNullOrEmpty(userName))
            {
                // 用户名不可空
                throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                    "Security token is not a valid username token. An empty username is not accepted.");
            }
            if (String.IsNullOrEmpty(password))
            // 密码不可空
            {
                throw ServiceResultException.Create(StatusCodes.BadIdentityTokenRejected,
                    "Security token is not a valid username token. An empty password is not accepted.");
            }
            if (userName == "sysadmin" && password == "demo")
            {
                return new SystemConfigurationIdentity(new UserIdentity(userNameIdentityToken));
            }

            if (!((userName == "user1" && password == "password") ||
                (userName == "user2" && password == "password1")))
            {
                TranslationInfo info = new TranslationInfo(
                    "InvalidPassword",
                    "en-US",
                    "Invalid username or password.",
                    userName);

                throw new ServiceResultException(new ServiceResult(
                    StatusCodes.BadUserAccessDenied,
                    "InvalidPassword",
                    LoadServerProperties().ProductUri,
                    new LocalizedText(info)));
            }
            return new UserIdentity(userNameIdentityToken);
        }

        #region Private Fields
        private ICertificateValidator m_userCertificateValidator;
        #endregion
    }
}
