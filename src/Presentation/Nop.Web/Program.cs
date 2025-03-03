using System.Net.Http;
using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Domain.Security;
using Nop.Services.Catalog;
using Nop.Unleash;
using Nop.Unleash.Common;
using Nop.Unleash.Service;
using Nop.Unleash.WebJob;
using Nop.Web.Framework.Infrastructure.Extensions;
using Autofac.Core.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Configuration.AddJsonFile(NopConfigurationDefaults.AppSettingsFilePath, true, true);
if (!string.IsNullOrEmpty(builder.Environment?.EnvironmentName))
{
    var path = string.Format(NopConfigurationDefaults.AppSettingsEnvironmentFilePath, builder.Environment.EnvironmentName);
    builder.Configuration.AddJsonFile(path, true, true);
}
builder.Configuration.AddEnvironmentVariables();

IConfiguration configuration = builder.Configuration;
//Add services to the application and configure service provider

builder.Services.ConfigureApplicationServices(builder);
builder.Services.AddUnleashServices(configuration);
builder.Services.AddHttpClient();


var app = builder.Build();
//app.UseHangfireDashboard("/dashboard");

UnleashAPI.AppSettingConfigure(app.Services.GetRequiredService<IConfiguration>());
ConfigurationHelper.AppSettingConfigure(app.Services.GetRequiredService<IConfiguration>());

//Todo : Cron.Daily
//RecurringJob.AddOrUpdate<IRecurringTasks>(
   //task => task.RecurringStockUpdate(), Cron.Minutely);
//RecurringJob.AddOrUpdate<IRecurringTasks>(
   //task => task.RecurringOrderStatusUpdate(), Cron.Minutely);

//Configure the application HTTP request pipeline
app.ConfigureRequestPipeline();
app.StartEngine();

app.Run();
