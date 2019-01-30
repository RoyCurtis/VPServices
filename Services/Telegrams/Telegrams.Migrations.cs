﻿using System;
using System.IO;

namespace VPServices.Services
{
    partial class Telegrams : IService
    {
        public void Migrate(VPServices app, int target)
        {
            this.connection = app.Connection;

            switch (target)
            {
                case 1:
                    migSetupSQLite(app);
                    migDatToSQLite(app);
                    break;
            }
        }

        void migSetupSQLite(VPServices app)
        {
            connection.CreateTable<sqlTelegram>();
            logger.Debug("Created SQLite tables for user telegrams");
        }

        void migDatToSQLite(VPServices app)
        {
            var fileTelegrams = "Telegrams.dat";

            if ( !File.Exists(fileTelegrams) )
                return;
            
            #region Telegram transaction
            connection.BeginTransaction();
            var lines = File.ReadAllLines(fileTelegrams);

            foreach ( var line in lines )
            {
                var parts = line.TerseSplit(',');
                connection.Insert(new sqlTelegram
                {
                    Read    = false,
                    Target      = parts[0],
                    Source  = parts[1],
                    Message = parts[2].Replace("%COMMA", ","),
                    When    = parts.Length == 4
                        ? DateTime.Parse(parts[3])
                        : DateTime.MinValue
                });
            }

            connection.Commit(); 
            #endregion

            var backup = fileTelegrams + ".bak";
            File.Move(fileTelegrams, backup);
            logger.Debug("Migrated .dat telegrams to SQLite; backed up to '{BackupFile}'", backup);
        }
    }
}
