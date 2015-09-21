using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeagueSharp;

namespace KurisuRiven
{
    public static class Updater
    {
        public static void UpdateCheck()
        {
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        using (var c = new WebClient())
                        {
                            var rawVersion =
                                c.DownloadString(
                                    "https://raw.githubusercontent.com/xKurisu/KurisuSolutions/master/KurisuRiven/Properties/AssemblyInfo.cs");

                            var match =
                                new Regex(
                                    @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                                    .Match(rawVersion);

                            if (match.Success)
                            {
                                var gitVersion =
                                    new Version(
                                        string.Format(
                                            "{0}.{1}.{2}.{3}",
                                            match.Groups[1],
                                            match.Groups[2],
                                            match.Groups[3],
                                            match.Groups[4]));

                                if (gitVersion != Program.Version)
                                {
                                    Game.PrintChat("<b>Kurisu's Riven</b> - Outdated & newer version available!");
                                    Game.PrintChat("<font color=\"#FF6666\">- Fixed E=>W in Jungleclear");
                                    Game.PrintChat("<font color=\"#FF6666\">- Added AoE Q farm as higher priority in Laneclear");
                                    Game.PrintChat("<font color=\"#FF6666\">- Added Ward + Q3 to Flee");
                                }
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
        }
    }
}