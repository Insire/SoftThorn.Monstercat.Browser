using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.GitVersioning;
using Nerdbank.GitVersioning;

namespace Build
{
    public sealed class BuildContext : FrostingContext
    {
        public const string Platform = "AnyCPU";
        public const string BuildConfiguration = "Release";

        public const string LicenseFilePath = @".\src";
        public const string SolutionPath = @".\SoftThorn.Monstercat.Browser.sln";
        public const string AssemblyInfoPath = @".\src\SharedAssemblyInfo.cs";

        public const string ResultsPath = @".\build\binaries";

        public VersionOracle GitVersion { get; }

        public ProjectConfig[] Projects { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            GitVersion = context.GitVersioningGetVersion();

            this.Information($"Provider: {context.BuildSystem().Provider}");
            this.Information($"Platform: {context.Environment.Platform.Family} ({(context.Environment.Platform.Is64Bit ? "x64" : "x86")})");

            this.Information($"Version: {GitVersion.SemVer2}");

            Projects = new ProjectConfig[]
            {
                new ProjectConfig(){ BinaryRootDirectory = ResultsPath, ProjectDirectory = @".\src\Wpf", ProjectFileName = "SoftThorn.Monstercat.Browser.Wpf.csproj", Name = "Wpf"},
                new ProjectConfig(){ BinaryRootDirectory = ResultsPath, ProjectDirectory = @".\src\Avalonia", ProjectFileName ="SoftThorn.Monstercat.Browser.Avalonia.csproj", Name = "Avalonia"},
            };
        }
    }
}
