﻿using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Hwi;
using BTCPayServer.Hwi.Deployment;
using BTCPayServer.Hwi.Transports;
using BTCPayServer.Vault;
using BTCPayServer.Vault.HWI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HwiServerExtensions
    {
        public static IServiceCollection AddHwiServer(this IServiceCollection services, Action<HwiServerOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services.AddCors();
            services.AddSingleton(HwiVersions.v1_0_3);
            services.AddHostedService<HwiDownloadTask>();
            services.AddScoped<HwiServer>();
            services.AddSingleton<ITransport>(provider =>
            {
                return new CliTransport()
                {
                    Logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerNames.HwiServerCli)
                };
            });
            if (configure != null)
                services.Configure(configure);
            return services;
        }

        public static IApplicationBuilder UseHwiServer(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
                throw new ArgumentNullException(nameof(applicationBuilder));
            applicationBuilder.Map(new PathString("/hwi-bridge/v1"), app =>
            {
                app.UseCors(policy => policy.AllowAnyOrigin().WithMethods("POST"));
                app.Run(async ctx =>
                {
                    await ctx.RequestServices.GetRequiredService<HwiServer>().Handle(ctx);
                });
            });
            return applicationBuilder;
        }
    }
}
