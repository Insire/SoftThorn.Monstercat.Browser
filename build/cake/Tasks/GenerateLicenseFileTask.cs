using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("GenerateLicenseFile")]
    public sealed class GenerateLicenseFileTask : FrostingTask<BuildContext>
    {
        private const string DotnetToolName = "dotnet-project-licenses";
        private const string DotnetToolVersion = "2.3.13";

        public override void Run(BuildContext context)
        {
            Install();
            Run();
            Uninstall();

            void Install()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .Append($"tool install --global {DotnetToolName} --version {DotnetToolVersion}")
                );

                context.StartProcess("dotnet", settings);
            }

            void Uninstall()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .Append($"tool uninstall --global {DotnetToolName} ")
                );

                context.StartProcess("dotnet", settings);
            }

            void Run()
            {
                foreach (var project in context.Projects)
                {
                    var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .AppendSwitchQuoted("-i", project.ProjectDirectory.FullPath)
                        .Append("-j")
                        .AppendSwitchQuoted("-f", project.ProjectDirectory.FullPath)
                    );

                    context.StartProcess(DotnetToolName, settings);
                }
            }
        }
    }
}
