﻿using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    class OpcuaManagement
    {
        public void CreateServerInstance()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "XiaoOpcua",
                ApplicationUri = Utils.Format(@"urn:{0}:XiaoOpcua", System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Server,
                ServerConfiguration = new ServerConfiguration()
                {
                    BaseAddresses = { "opc.tcp://localhost:8020/", "https://localhost:8021/" },
                    MinRequestThreadCount = 5,
                    MaxRequestThreadCount = 100,
                    MaxQueuedRequestCount = 200
                },
                SecurityConfiguration = new SecurityConfiguration()
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = Utils.Format(@"CN={0}, DC={1}", "AxiuOpcua", System.Net.Dns.GetHostName()) },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()                
            };
            config.Validate(ApplicationType.Server).GetAwaiter().GetResult();
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            var application = new ApplicationInstance
            {
                ApplicationName = "XiaoOpcua",
                ApplicationType = ApplicationType.Server,
                ApplicationConfiguration = config
            };
            bool certOk = application.CheckApplicationInstanceCertificate(false, 0).Result;
            if (!certOk)
            {
                Console.WriteLine("证书验证失败");
            }

            var dis = new DiscoveryServerBase();
            application.Start(new XOpcuaServer());

        }
    }
}
