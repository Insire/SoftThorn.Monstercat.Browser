using Cake.Core.IO;

namespace Build
{
    public sealed class ProjectConfig
    {
        public DirectoryPath ProjectDirectory { get; init; } = default!;

        public string ProjectFileName { get; init; } = default!;

        public string Name { get; init; } = default!;

        public DirectoryPath BinaryDirectory => BinaryRootDirectory.Combine(Name);

        public DirectoryPath BinaryRootDirectory { get; init; } = default!;

        public FilePath ProjectFilePath => ProjectDirectory.CombineWithFilePath(ProjectFileName);
    }
}
