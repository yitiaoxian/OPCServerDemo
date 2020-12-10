using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    class DiscoveryServer : StandardServer,IDiscoveryServer
    {
        private ServerInternalData m_serverInternal;
        /// <summary>
        /// 清理掉线客户端定时器
        /// </summary>
        private static Timer m_timer = null;
        /// <summary>
        /// 已注册服务端列表
        /// </summary>
        List<RegisteredServerTable> _serverTable = new List<RegisteredServerTable>();

        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties serverProperties = new ServerProperties();
            serverProperties.ManufacturerName = "OPC Foundation";
            serverProperties.ProductName = "CET Discovery Server";
            serverProperties.ProductUri = "http://opcfoundation.org/Quickstart/ReferenceServer/v1.04";
            serverProperties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
            serverProperties.BuildNumber = Utils.GetAssemblyBuildNumber();
            serverProperties.BuildDate = Utils.GetAssemblyTimestamp();
            
            return serverProperties;
        }
        /// <summary>
        /// 获取终结点实例
        /// 此方法必须重写
        /// discovery服务端必须创建DiscoveryEndpoint终点，实时数据服务端必须创建SessionEndpoint终点
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        protected override EndpointBase GetEndpointInstance(ServerBase server)
        {
            return new DiscoveryEndpoint(server);
        }

        protected override void StartApplication(ApplicationConfiguration configuration)
        {
            lock (m_lock)
            {
                try
                {
                    m_serverInternal = new ServerInternalData(
                                ServerProperties,
                                configuration,
                                MessageContext,
                                new CertificateValidator(),
                                InstanceCertificate);
                    //创建负责提供本地化字符串资源的管理器
                    ResourceManager resourceManager = CreateResourceManager(m_serverInternal, configuration);
                    //创建负责管理请求的管理器
                    RequestManager requestManager = new RequestManager(m_serverInternal);
                    //创建主节点管理器
                    MasterNodeManager masterNodeManager = new MasterNodeManager(m_serverInternal, configuration, null);
                    //将节点管理器添加到数据存储中
                    m_serverInternal.SetNodeManager(masterNodeManager);
                    //将节点管理器置于允许其他对象使用的状态。
                    masterNodeManager.Startup();
                    //创建事件管理器
                    EventManager eventManager = new EventManager(m_serverInternal, (uint)configuration.ServerConfiguration.MaxEventQueueSize);
                    m_serverInternal.CreateServerObject(eventManager, resourceManager, requestManager);

                    m_serverInternal.AggregateManager = CreateAggregateManager(m_serverInternal, configuration);
                    SessionManager sessionManager = new SessionManager(m_serverInternal, configuration);
                    sessionManager.Startup();
                    //启动订阅管理器
                    SubscriptionManager subscriptionManager = new SubscriptionManager(m_serverInternal, configuration);
                    subscriptionManager.Startup();
                    //将会话管理器添加到数据存储
                    m_serverInternal.SetSessionManager(sessionManager, subscriptionManager);

                    ServerError = null;
                    //将server设置为running
                    SetServerState(ServerState.Running);

                    //检测配置文件
                    if (!String.IsNullOrEmpty(configuration.SourceFilePath))
                    {
                        var m_configurationWatcher = new ConfigurationWatcher(configuration);
                        //事件委托
                        m_configurationWatcher.Changed += new EventHandler<ConfigurationWatcherEventArgs>(this.OnConfigurationChanged);
                    }
                    CertificateValidator.CertificateUpdate += OnCertificateUpdate;
                    //60s后开始清理过期服务列表,此后每60s检查一次
                    m_timer = new Timer(ClearNoliveServer, null, 60000, 60000);
                    Console.WriteLine("Discovery服务已启动完成,请勿退出程序!!!");
                }
                catch (Exception e)
                {

                    Utils.Trace(e, "Unexpected error starting application");
                    m_serverInternal = null;
                    ServiceResult error = ServiceResult.Create(e, StatusCodes.BadInternalError, "Unexpected error starting application");
                    ServerError = error;
                    throw new ServiceResultException(error);
                }
            }            
        }
        /// <summary>
        /// 初始化监听服务（可以不重写）
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="serverDescription"></param>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        protected override IList<Task> InitializeServiceHosts(ApplicationConfiguration configuration,
            out ApplicationDescription serverDescription,
            out EndpointDescriptionCollection endpoints)
        {
            serverDescription = null;
            endpoints = null;

            Dictionary<string, Task> hosts = new Dictionary<string, Task>();

            //确保至少有一个security policy
            if (configuration.ServerConfiguration.SecurityPolicies.Count == 0)
            {
                configuration.ServerConfiguration.SecurityPolicies.Add(new ServerSecurityPolicy());
            }
            //确保至少有一个user token
            if (configuration.ServerConfiguration.UserTokenPolicies.Count == 0)
            {
                UserTokenPolicy userTokenPolicy = new UserTokenPolicy();

                userTokenPolicy.TokenType = UserTokenType.Anonymous;
                userTokenPolicy.PolicyId = userTokenPolicy.TokenType.ToString();

                configuration.ServerConfiguration.UserTokenPolicies.Add(userTokenPolicy);
            }

            // 设置server 描述.
            serverDescription = new ApplicationDescription();

            serverDescription.ApplicationUri = configuration.ApplicationUri;
            serverDescription.ApplicationName = new LocalizedText("en-US", configuration.ApplicationName);
            serverDescription.ApplicationType = configuration.ApplicationType;
            serverDescription.ProductUri = configuration.ProductUri;
            serverDescription.DiscoveryUrls = GetDiscoveryUrls();

            endpoints = new EndpointDescriptionCollection();
            IList<EndpointDescription> endpointsForHost = null;

            // 创建 UA TCP host.
            endpointsForHost = CreateUaTcpServiceHost(
                hosts,
                configuration,
                configuration.ServerConfiguration.BaseAddresses,
                serverDescription,
                configuration.ServerConfiguration.SecurityPolicies);

            endpoints.InsertRange(0, endpointsForHost);

            //创建https host
#if !NO_HTTPS
            endpointsForHost = CreateHttpsServiceHost(
                hosts,
                configuration,
                configuration.ServerConfiguration.BaseAddresses,
                serverDescription,
                configuration.ServerConfiguration.SecurityPolicies);
            endpoints.AddRange(endpointsForHost);
#endif
            return new List<Task>(hosts.Values);
        }

        ///218
    }
}
