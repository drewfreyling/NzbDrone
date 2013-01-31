﻿using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;
using NLog;

namespace NzbDrone.Core.Datastore
{
    public class MigrationsHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        public static void Run(string connetionString, bool trace)
        {
            EnsureDatabase(connetionString);

            Logger.Trace("Preparing to run database migration");

            try
            {
                Migrator.Migrator migrator;
                if (trace)
                {
                    migrator = new Migrator.Migrator("sqlserverce", connetionString, Assembly.GetAssembly(typeof(MigrationsHelper)), true, new MigrationLogger());
                }
                else
                {
                    migrator = new Migrator.Migrator("sqlserverce", connetionString, Assembly.GetAssembly(typeof(MigrationsHelper)));
                }



                migrator.MigrateToLastVersion();
                Logger.Info("Database migration completed");


            }
            catch (Exception e)
            {
                Logger.FatalException("An error has occurred while migrating database", e);
                throw;
            }
        }

        private static void EnsureDatabase(string constr)
        {
            var connection = new SqlCeConnection(constr);

            if (!File.Exists(connection.Database))
            {
                var engine = new SqlCeEngine(constr);
                engine.CreateDatabase();
            }
        }

        public static string GetIndexName(string tableName, params string[] columns)
        {
            return String.Format("IX_{0}_{1}", tableName, String.Join("_", columns));
        }
    }




}