using System;
using System.Collections.Generic;
using System.Linq;
using Ionic.Zip;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Model;

namespace NzbDrone.Core.Providers
{
    public class BackupProvider
    {
        private readonly EnvironmentProvider _environmentProvider;
        private readonly DiskProvider _diskProvider;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      
        public BackupProvider(EnvironmentProvider environmentProvider, DiskProvider diskProvider)
        {
            _environmentProvider = environmentProvider;
            _diskProvider = diskProvider;
        }

        public BackupProvider()
        {
            
        }

        public virtual string CreateBackupZip()
        {
            var dbFiles = new List<string>();

            foreach (var value in Enum.GetValues(typeof(DatabaseType)))
            {
                var dbFile = _environmentProvider.GetNzbDroneDbFile((DatabaseType)value);

                if (_diskProvider.FileExists(dbFile))
                    dbFiles.Add(dbFile);
            }
           
            var configFile = _environmentProvider.GetConfigPath();
            var zipFile = _environmentProvider.GetConfigBackupFile();

            using (var zip = new ZipFile())
            {
                zip.AddFiles(dbFiles, String.Empty);
                zip.AddFile(configFile, String.Empty);
                zip.Save(zipFile);
            }

            return zipFile;
        }
    }
}
