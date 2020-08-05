using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSearchDemo.Helpers
{
    public interface ICustomConfigurationSettings
    {
        public Logging Logging { get; set; }
        public string AllowedHosts { get; set; }
        public string AzureSearchName { get; set; }
        public string AzureSearchAdminKey { get; set; }
        public string AzureSearchQueryApiKey { get; set; }
        public string TestAppConfig { get; set; }
        public Testazurekey Testazurekey { get; set; }
        public Serilog Serilog { get; set; }
    }
    public class CustomConfigurationSettings : ICustomConfigurationSettings
    {

        public Logging Logging { get; set; }
        public string AllowedHosts { get; set; }
        public string AzureSearchName { get; set; }
        public string AzureSearchAdminKey { get; set; }
        public string AzureSearchQueryApiKey { get; set; }
        public string TestAppConfig { get; set; }
        public Testazurekey Testazurekey { get; set; }
        public Serilog Serilog { get; set; }

    }

    public class Logging
    {
        public Loglevel LogLevel { get; set; }
    }

    public class Loglevel
    {
        public string Default { get; set; }
        public string Microsoft { get; set; }
        public string MicrosoftHostingLifetime { get; set; }
    }

    public class Testazurekey
    {
        public string Testquerykey { get; set; }
        //public object[] Destructure { get; set; }
        //public Properties Properties { get; set; }
    }

    public class Serilog
    {
        public string[] Using { get; set; }
        public Minimumlevel MinimumLevel { get; set; }
        public Writeto[] WriteTo { get; set; }
        public string[] Enrich { get; set; }
        public object[] Destructure { get; set; }
        public Properties Properties { get; set; }
    }

    public class Minimumlevel
    {
        public string Default { get; set; }
        public Override Override { get; set; }
    }

    public class Override
    {
        public string Microsoft { get; set; }
        public string System { get; set; }
    }

    public class Properties
    {
        public string Application { get; set; }
    }

    public class Writeto
    {
        public string Name { get; set; }
        public Args Args { get; set; }
    }

    public class Args
    {
        public string outputTemplate { get; set; }
        public string serverUrl { get; set; }
    }
}
