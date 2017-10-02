﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using VpNet;
using VPServices.Extensions;

namespace VPServices.Services
{
    /// <summary>
    /// Handles general utility commands
    /// </summary>
    public class GeneralCommands : IService
    {
        public string Name
        {
            get { return "General commands"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Services: Help", @"^(help|commands|\?)$", cmdHelp,
                    @"Prints the URL to this documentation to all users or explains a specific command to you",
                    @"!help `[command]`"
                ),

                new Command
                (
                    "Services: Version", @"^version$", cmdVersion,
                    @"Sends all users the version of this bot",
                    @"!version", 120
                ),

                new Command
                (
                    "Info: Position", "^(my)?(coord(s|inates)?|pos(ition)?|compass)$", cmdCoords,
                    @"Prints user's position to chat, including coordinates, pitch, yaw and compass",
                    @"!pos"
                ),

                new Command
                (
                    "Info: Data", "^(my)?(data|settings)$", cmdData,
                    @"Prints a listing of user's settings saved by the bot",
                    @"!mydata"
                ),

                new Command
                (
                    "Teleport: Offset X", "^x$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.X); },
                    @"Offsets user's X coordinate by *x* number of meters",
                    @"!x `x`"
                ),

                new Command
                (
                    "Teleport: Offset altitude", "^(alt|y)$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.Y); },
                    @"Offsets user's altitude by *x* number of meters",
                    @"!alt `x`"
                ),

                new Command
                (
                    "Teleport: Offset Z", "^z$",
                    (s,a,d) => { return cmdOffset(a, d, offsetBy.Z); },
                    @"Offsets user's Z coordinate by *x* number of meters",
                    @"!z `z`"
                ),

                new Command
                (
                    "Teleport: Random", "^rand(om)?$", cmdRandomPos,
                    @"Teleports user to a random X,Z coordinate within the 65535 / -65535 range at ground level",
                    @"!rand"
                ),

                new Command
                (
                    "Teleport: Ground zero", "^g(round)?z(ero)?$", cmdGroundZero,
                    @"Teleports user to ground zero (0,0,0)",
                    @"!gz"
                ),

                new Command
                (
                    "Teleport: Ground", "^g(round)?$", cmdGround,
                    @"Snaps the user to ground",
                    @"!g"
                ),

                new Command
                (
                    "Debug: Say", "^say$", cmdSay,
                    @"Makes the bot say a chat message as somebody else",
                    @"!say `who: message`"
                ),

                new Command
                (
                    "Debug: Crash", "^(crash|exception)$", cmdCrash,
                    @"Crashes the bot for debugging purposes; owner only",
                    @"!crash"
                ),

                new Command
                (
                    "Debug: Hang", "^hang$", cmdHang,
                    @"Hangs the bot for debugging purposes; owner only",
                    @"!hang"
                ),
            });

            app.Routes.Add(new WebRoute("Help", "^(help|commands)$", webHelp,
                @"Provides documentation on using the bot in-world via chat"));
        }

        public void Migrate(VPServices app, int target) {  }
        public void Dispose() { }

        enum offsetBy
        {
            X, Y, Z
        }

        const string msgCommandTitle   = "*** {0}";
        const string msgCommandRgx     = "Regex: {0}";
        const string msgCommandDesc    = "Description: {0}";
        const string msgCommandExample = "Example: {0}";

        #region Services commands
        bool cmdHelp(VPServices app, Avatar<Vector3> who, string data)
        {
            var helpUrl = app.PublicUrl + "help";

            if ( data != "" )
            {
                // If given data, try to find specific command and print help in console for
                // that user
                foreach ( var cmd in app.Commands )
                    if ( TRegex.IsMatch(data, cmd.Regex) )
                    {
                        app.Bot.ConsoleMessage(who.Session, "", string.Format(msgCommandTitle, cmd.Name), VPServices.ColorInfo, TextEffectTypes.BoldItalic);
                        app.Bot.ConsoleMessage(who.Session, "", string.Format(msgCommandRgx, cmd.Regex), VPServices.ColorInfo, TextEffectTypes.Italic);
                        app.Bot.ConsoleMessage(who.Session, "", string.Format(msgCommandDesc, cmd.Help), VPServices.ColorInfo, TextEffectTypes.Italic);
                        app.Bot.ConsoleMessage(who.Session, "", string.Format(msgCommandExample, cmd.Example), VPServices.ColorInfo, TextEffectTypes.Italic);

                        return true;
                    }

                app.Warn(who.Session, "Could not match any command for '{0}'; try {1}", data, helpUrl);
                return true;
            }
            else
            {
                // Broadcast help URL for everybody
                app.NotifyAll("Command help can be found at {0}", helpUrl);
                return true;
            }
        }

        bool cmdVersion(VPServices app, Avatar<Vector3> who, string data)
        {
            var asm      = Assembly.GetExecutingAssembly().Location;
            var fileDate = File.GetLastWriteTime(asm);

            app.NotifyAll("I was built on {0}", fileDate);
            return true;
        } 

        const string msgDataResults   = "*** User settings stored for you:";
        const string msgDataNoResults = "No user data is stored for you";
        const string msgDataResult    = "{0}: {1}";

        bool cmdData(VPServices app, Avatar<Vector3> who, string data)
        {
            var settings = who.GetSettings();

            if (settings.Count <= 0)
            {
                app.Notify(who.Session, msgDataNoResults);
                return true;
            }

            app.Bot.ConsoleMessage(who.Session, "", msgDataResults, VPServices.ColorInfo, TextEffectTypes.BoldItalic);
            foreach (var s in settings)
                app.Bot.ConsoleMessage(who.Session, "", string.Format(msgDataResult, s.Key, s.Value), VPServices.ColorInfo, TextEffectTypes.Italic);

            return true;
        } 
        #endregion

        #region Debug commands
        bool cmdCrash(VPServices app, Avatar<Vector3> who, string data)
        {
            var owner = app.NetworkSettings.Get("Username");

            if ( !who.Name.IEquals(owner) )
                return false;

            app.Crash = true;
            return true;
        }

        bool cmdHang(VPServices app, Avatar<Vector3> who, string data)
        {
            var owner = app.NetworkSettings.Get("Username");

            if ( !who.Name.IEquals(owner) )
                return false;
            else
            {
                var test = 0;
                while (true) test++;
            }
        }

        bool cmdSay(VPServices app, Avatar<Vector3> who, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+)$");
            if ( !matches.Success )
                return false;

            app.Bot.ConsoleMessage($"\"{matches.Groups[1].Value.Trim()}\"", $"{matches.Groups[2].Value.Trim()}", new Color(128,128,128));
            return true;
        }

        #endregion

        #region Teleport commands
        bool cmdCoords(VPServices app, Avatar<Vector3> who, string data)
        {
            var compass = CompassExtensions.ToCompassTuple(who);

            app.Notify(who.Session, "You are at X: {0:f4} Y: {1:f4}a Z: {2:f4}, facing {3} ({4:f0}), pitch {5:f0}", who.Position.X, who.Position.Y, who.Position.Z, compass.Direction, compass.Angle, who.Rotation.X);
            return true;
        }

        bool cmdOffset(Avatar<Vector3> who, string data, offsetBy offsetBy)
        {
            float   by;
            Vector3 location;
            if ( !float.TryParse(data, out by) )
                return false;

            switch (offsetBy)
            {
                default:
                case offsetBy.X:
                    location = new Vector3(who.Position.X + by, who.Position.Y, who.Position.Z);
                    break;
                case offsetBy.Y:
                    location = new Vector3(who.Position.X, who.Position.Y + by, who.Position.Z);
                    break;
                case offsetBy.Z:
                    location = new Vector3(who.Position.X, who.Position.Y, who.Position.Z + by);
                    break;
            }

            VPServices.App.Bot.TeleportAvatar(who.Session, "", location, who.Rotation.Y, who.Rotation.X);
            return true;
        }

        bool cmdRandomPos(VPServices app, Avatar<Vector3> who, string data)
        {
            var randX = VPServices.Rand.Next(-65535, 65535);
            var randZ = VPServices.Rand.Next(-65535, 65535);

            app.Notify(who.Session, "Teleporting to {0}, 0, {1}", randX, randZ);
            app.Bot.TeleportAvatar(who.Session, "", new Vector3(randX, 0, randZ), who.Rotation.Y, who.Rotation.X);
            return true;
        } 

        bool cmdGroundZero(VPServices app, Avatar<Vector3> who, string data)
        {
            app.Bot.TeleportAvatar(who.Session, "", new Vector3(), 0, 0);
            return true;
        } 

        bool cmdGround(VPServices app, Avatar<Vector3> who, string data)
        {
            app.Bot.TeleportAvatar(who.Session, "", new Vector3(who.Position.X, 0.1f, who.Position.Z), who.Rotation.Y, who.Rotation.X);
            return true;
        }
        #endregion

        #region Web routes
        string webHelp(VPServices app, string data)
        {
            string listing = "# Bot commands available:\n";

            foreach (var command in app.Commands)
            {
                listing +=
@"## {0}

* **Regex:** {1}
* **Example:** {2}
* **Time limit:** {3}
* *{4}*

".LFormat(command.Name, command.Regex, command.Example, command.TimeLimit, command.Help);
            }

            return app.MarkdownParser.Transform(listing);
        }
        #endregion
    }
}
