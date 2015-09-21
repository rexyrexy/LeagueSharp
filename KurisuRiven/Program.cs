using System;
using System.Reflection;
using LeagueSharp.Common;

namespace KurisuRiven
{
    internal static class Program
    {
        public static System.Version Version;   
        private static void Main(string[] args)
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
            new KurisuRiven();
        }
    }
}