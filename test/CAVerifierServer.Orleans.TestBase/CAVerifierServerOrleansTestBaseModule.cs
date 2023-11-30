using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace CAVerifierServer;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(CAVerifierServerDomainModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class CAVerifierServerOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<VerifierAccountOptions>(o =>
        {
            o.PrivateKey = "XXXXXXXX";
            o.Address = "XXXXXXX";
            o.KeyStorePath = "keystore file path";
            o.KeyStorePassword = "keystore file password";
        });
        context.Services.Configure<VerifierCodeOptions>(o =>
        {
            o.RetryTimes = 2;
            o.CodeExpireTime = 5;
            o.GetCodeFrequencyLimit = 1;
            o.GetCodeFrequencyTimeLimit = 1;
        });
    }
}