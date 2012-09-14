using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Data.SqlServerCe;
using NzbDrone.Common;
using NzbDrone.Common.Model;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace NzbDrone.Core.Datastore
{
    class DbProviderFactory : System.Data.Common.DbProviderFactory
    {
        public Boolean IsProfiled { get; set; }
        public DatabaseType DatabaseType { get; set; }

        public override DbConnection CreateConnection()
        {
            if (DatabaseType == DatabaseType.SQLite)
            {
                var sqliteConnection = new SQLiteConnection();
                DbConnection connection = sqliteConnection;

                if (IsProfiled)
                {
                    connection = new ProfiledDbConnection(sqliteConnection, MiniProfiler.Current);
                }

                return connection;
            }

            else
            {
                var sqlCeConnection = new SqlCeConnection();
                DbConnection connection = sqlCeConnection;

                if (IsProfiled)
                {
                    connection = new ProfiledDbConnection(sqlCeConnection, MiniProfiler.Current);
                }

                return connection;
            }
        }
    }
}
