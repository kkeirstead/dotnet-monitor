// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class OperationExtensions
    {
        public static IServiceCollection ConfigureOperationStore(this IServiceCollection services)
        {
            services.AddSingleton<IEgressOperationQueue, EgressOperationQueue>();
            services.AddSingleton<EgressOperationStore>();
            services.AddHostedServiceForwarder<EgressOperationService>();
            services.AddSingleton<EgressOperationService>();
            return services;
        }

        // temporary copy paste
        public static void AddHostedServiceForwarder<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddHostedService<THostedService>(sp => sp.GetRequiredService<THostedService>());
        }
    }
}
