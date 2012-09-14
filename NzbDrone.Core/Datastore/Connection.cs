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
        private readonly ConfigFileProvider _configFileProvider;

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

        public Connection(EnvironmentProvider environmentProvider, ConfigFileProvider configFileProvider)
        {
            _environmentProvider = environmentProvider;
            _configFileProvider = configFileProvider;
        }

        public String MainConnectionString
        {
            get
            {
                var databaseType = _configFileProvider.DatabaseType;
                return GetConnectionString(_environmentProvider.GetNzbDroneDbFile(databaseType), databaseType);
            }
        }

        public String LogConnectionString
        {
            get
            {
                var databaseType = _configFileProvider.DatabaseType;
                //if (databaseType == DatabaseType.SQLite)
                //{
                //    var connectionString = _environmentProvider.GetLogDbFileDbFile(databaseType);
                //    return
                //            GetConnectionString(
                //                                String.Format(
                //                                              "metadata=res://*/datacon.csdl|res://*/datacon.ssdl|res://*/datacon.msl;provider=System.Data.SQLite;provider connection string='{0}'",
                //                                              connectionString), databaseType);
                //}

                return GetConnectionString(_environmentProvider.GetLogDbFileDbFile(databaseType), databaseType); 
            }
        }

        public static string GetConnectionString(string path, DatabaseType databaseType = DatabaseType.SQLCE)
        {
            if (databaseType == DatabaseType.SQLite)
                return String.Format("Data Source=\"{0}\"; Version = 3", path);

            return String.Format("Data Source=\"{0}\"; Max Database Size = 512;", path);
        }

        public IDatabase GetMainPetaPocoDb(Boolean profiled = true)
        {
            return GetPetaPocoDb(MainConnectionString, _configFileProvider.DatabaseType, profiled);
        }

        public IDatabase GetLogPetaPocoDb(Boolean profiled = true)
        {
            return GetPetaPocoDb(LogConnectionString, _configFileProvider.DatabaseType, profiled);
        }

        public LogDbContext GetLogEfContext()
        {
            return GetLogDbContext(LogConnectionString, _configFileProvider.DatabaseType);
        }

        public static IDatabase GetPetaPocoDb(string connectionString, DatabaseType databaseType = DatabaseType.SQLCE, Boolean profiled = true)
        {
            MigrationsHelper.Run(connectionString, databaseType, true);

            var factory = new DbProviderFactory
                              {
                                  IsProfiled = profiled
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

        public static LogDbContext GetLogDbContext(string connectionString, DatabaseType databaseType = DatabaseType.SQLCE)
        {
            MigrationsHelper.Run(connectionString, databaseType, true);

            if (databaseType == DatabaseType.SQLite)
            {
                DbConnection sqliteConnection = new SQLiteConnection(connectionString);
                return new LogDbContext(sqliteConnection);
            }

            DbConnection sqlCeConnection = new SqlCeConnection(connectionString);
            return new LogDbContext(sqlCeConnection);
        }
    }
}
