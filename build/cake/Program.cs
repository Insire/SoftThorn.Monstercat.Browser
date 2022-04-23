using Cake.Frosting;
using System;

namespace Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .UseWorkingDirectory("../../")
                .SetToolPath("../")
                .InstallTool(new Uri("nuget:?package=GitVersion.CommandLine&version=5.10.1"))
                .Run(args);
        }
    }
}