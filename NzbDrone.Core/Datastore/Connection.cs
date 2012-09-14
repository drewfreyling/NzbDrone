using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SQLite;
using System.Data.SqlServerCe;
using NzbDrone.Common;
using NzbDrone.Common.Model;
using NzbDrone.Core.Instrumentation;
using PetaPoco;

namespace NzbDrone.Core.Datastore
{
    public class Connection
    {
        private readonly EnvironmentProvider _environmentProvider;

        static Connection()
        {
            Database.Mapper = new CustomeMapper();

            var dataSet = ConfigurationManager.GetSection("system.data") as System.Data.DataSet;
            dataSet.Tables[0].Rows.Add("Microsoft SQL Server Compact Data Provider 4.0"
            , "System.Data.SqlServerCe.4.0"
            , ".NET Framework Data Provider for Microsoft SQL Server Compact"
            , "System.Data.SqlServerCe.SqlCeProviderFactory, System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");

            dataSet.Tables[0].Rows.Add("SQLite Data Provider"
            , "System.Data.SQLite"
            , ".Net Framework Data Provider for SQLite"
            , "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
        }

        public Connection(EnvironmentProvider environmentProvider)
        {
            _environmentProvider = environmentProvider;
        }

        public String MainConnectionString(DatabaseType databaseType = DatabaseType.SQLCE)
        {
            return GetConnectionString(_environmentProvider.GetNzbDroneDbFile(databaseType), databaseType);
        }

        public String LogConnectionString(DatabaseType databaseType = DatabaseType.SQLCE)
        {
            return GetConnectionString(_environmentProvider.GetLogDbFileDbFile(databaseType), databaseType);
        }

        public static string GetConnectionString(string path, DatabaseType databaseType = DatabaseType.SQLCE)
        {
            if (databaseType == DatabaseType.SQLite)
                return String.Format("Data Source=\"{0}\"; Version = 3", path);

            return String.Format("Data Source=\"{0}\"; Max Database Size = 512;", path);
        }

        public IDatabase GetMainPetaPocoDb(DatabaseType databaseType = DatabaseType.SQLCE, Boolean profiled = true) 
        {
            return GetPetaPocoDb(MainConnectionString(databaseType), databaseType, profiled);
        }

        public IDatabase GetLogPetaPocoDb(DatabaseType databaseType = DatabaseType.SQLCE, Boolean profiled = true)
        {
            return GetPetaPocoDb(LogConnectionString(databaseType), databaseType, profiled);
        }

        public static IDatabase GetPetaPocoDb(string connectionString, DatabaseType databaseType = DatabaseType.SQLCE, Boolean profiled = true)
        {
            MigrationsHelper.Run(connectionString, databaseType, true);

            var factory = new DbProviderFactory
                              {
                                  IsProfiled = profiled,
                                  DatabaseType = databaseType
                              };

            var petaPocoDbType = Database.DBType.SqlServerCE;
            if (databaseType == DatabaseType.SQLite) petaPocoDbType = Database.DBType.SQLite;

            var db = new Database(connectionString, factory, petaPocoDbType)
                         {
                             KeepConnectionAlive = true,
                             ForceDateTimesToUtc = false,
                         };

            return db;
        }
    }
}
