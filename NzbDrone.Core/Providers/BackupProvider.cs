using System;
using System.Linq;
using Ionic.Zip;
using NLog;
using Ninject;
using NzbDrone.Common;

namespace NzbDrone.Core.Providers
{
    public class BackupProvider
    {
        private readonly EnvironmentProvider _environmentProvider;
        private readonly ConfigFileProvider _configFileProvider;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      
        [Inject]
        public BackupProvider(EnvironmentProvider environmentProvider, ConfigFileProvider configFileProvider)
        {
            _environmentProvider = environmentProvider;
            _configFileProvider = configFileProvider;
        }

        public BackupProvider()
        {
            
        }

        public virtual string CreateBackupZip()
        {
            var dbFile = _environmentProvider.GetNzbDroneDbFile(_configFileProvider.DatabaseType);
            var configFile = _environmentProvider.GetConfigPath();
            var zipFile = _environmentProvider.GetConfigBackupFile();

            using (var zip = new ZipFile())
            {
                zip.AddFile(dbFile, String.Empty);
                zip.AddFile(configFile, String.Empty);
                zip.Save(zipFile);
            }

            return zipFile;
        }
    }
}
