using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Nop.Unleash.Common
{
    public static class ConfigurationHelper
    {
        private static IConfiguration _configuration;
        private const string appsetting = "appsettings.json";

        public static void AppSettingConfigure(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string Setting(string key)
        {
            _configuration = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile(appsetting)
                            .Build();

            return _configuration.GetSection(key).Value;
        }

        public static string GetConnectionStringWithoutTrust(string connectionString)
        {
            var pairs = connectionString.Split(';');
            var stringBuilder = new StringBuilder();

            foreach (var pair in pairs)
            {
                if (!pair.Contains("Trust Server Certificate=True"))
                {
                    stringBuilder.Append(pair + ";");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
