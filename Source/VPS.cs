﻿using System;
using System.Text.RegularExpressions;
using System.Threading;
using VP;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public static Random   Rand = new Random();
        public static Instance Bot;
        public static string   Owner;
        public static bool     Crash;

        public static int Main(string[] args)
        {
            // Set up logger
            new ConsoleLogger();
            Console.WriteLine("### [{0}] Services is starting...", DateTime.Now);

            try
            {
                App = new VPServices();
                App.SetupSettings(args);
                App.Setup();

                while (true)
                    App.UpdateLoop();
            }
            catch (Exception e)
            {
                e.LogFullStackTrace();
                return 1;
            }

            App.Dispose();
            Console.WriteLine("### [{0}] Services is now exiting", DateTime.Now);
            return exit;
        }

        /// <summary>
        /// Sets up bot instance
        /// </summary>
        public void Setup()
        {
            // Set logging level
            LogLevels logLevel;
            Enum.TryParse<LogLevels>( CoreSettings.Get("LogLevel", "Production"), out logLevel );
            Log.Level = logLevel;

            // Load instance
            Bot      = new Instance( CoreSettings.Get("Name", defaultName) );
            userName = NetworkSettings.Get("Username");
            password = NetworkSettings.Get("Password");
            World    = NetworkSettings.Get("World");
            Owner    = userName;

            // Connect to network
            ConnectToUniverse();
            Log.Info("Network", "Connected to universe");

            // Set up subsystems
            SetupDatabase();
            SetupCommands();
            SetupEvents();
            LoadServices();

            // Set up services
            ConnectToWorld();
            PerformMigrations();
            InitServices();
            Log.Info("Network", "Connected to {0}", World);

            CoreSettings.Set("Version", MigrationVersion);
            Bot.ConsoleBroadcast(ChatEffect.None, ColorInfo,"", "Services is now online; say !help for information");
        }

        /// <summary>
        /// Pumps bot events
        /// </summary>
        public void UpdateLoop()
        {
            Bot.Wait(0);

            if (Crash)
            {
                Crash = false;
                throw new Exception("Forced crash in update loop");
            }

            Thread.Sleep(100);
        }

        /// <summary>
        /// Disposes of application by clearing all loaded service and disposing of bot
        /// </summary>
        public void Dispose()
        {
            Commands.Clear();

            ClearEvents();
            ClearServices();
            CloseDatabase();
            Bot.Dispose();
        }

        #region Helper functions
        public static bool TryParseBool(string msg, out bool value)
        {
            if ( TRegex.IsMatch(msg, "^(true|1|yes|on)$") )
            {
                value = true;
                return true;
            }
            else if ( TRegex.IsMatch(msg, "^(false|0|no|off)$") )
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        public void Notify(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, ChatEffect.Italic, ColorInfo, Bot.Name, msg, parts);
        }

        public void NotifyAll(string msg, params object[] parts)
        {
            Notify(0, msg, parts);
        }

        public void Alert(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, ChatEffect.Bold, ColorAlert, Bot.Name, msg, parts);
        }

        public void AlertAll(string msg, params object[] parts)
        {
            Alert(0, msg, parts);
        }

        public void Warn(int session, string msg, params object[] parts)
        {
            Bot.ConsoleMessage(session, ChatEffect.Italic, ColorWarn, Bot.Name, msg, parts);
        }

        public void WarnAll(string msg, params object[] parts)
        {
            Warn(0, msg, parts);
        }
        #endregion
    }
}
