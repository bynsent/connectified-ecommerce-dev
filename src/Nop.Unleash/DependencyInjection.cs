using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Services.Catalog;
using Nop.Unleash.Common;
using Nop.Unleash.Service;
using Nop.Unleash.WebJob;

namespace Nop.Unleash
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUnleashServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProductUnleashService, ProductUnleashService>();
            services.AddScoped<IProductService, ProductService>();
            //services.AddSingleton<IRecurringTasks, RecurringTasks>();

            //services.AddHangfire(x =>
            //{
            //    x.UseSqlServerStorage(ConfigurationHelper.GetConnectionStringWithoutTrust(configuration.GetConnectionString(Constant.CONNECTION_STRING)));
            //});

            //services.AddHangfireServer();
            return services;
        }
    }
}
