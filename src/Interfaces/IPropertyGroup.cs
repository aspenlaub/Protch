namespace Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

public interface IPropertyGroup {
    string AssemblyName { get; set; }
    string Condition { get; set; }
    string RootNamespace { get; set; }
    string RepositoryType { get; set; }
    string RepositoryUrl { get; set; }
    string RepositoryBranch { get; set; }
    string PackageId { get; set; }
    string IntermediateOutputPath { get; set; }
    string OutputPath { get; set; }
    string UseVsHostingProcess { get; set; }
    string GenerateBuildInfoConfigFile { get; set; }
    string AppendTargetFrameworkToOutputPath { get; set; }
    string AllowUnsafeBlocks { get; set; }
    string NuspecFile { get; set; }
    string Deterministic { get; set; }
    string GenerateAssemblyInfo { get; set; }
}