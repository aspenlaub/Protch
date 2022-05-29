// ReSharper disable UnusedMember.Global
namespace Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

public interface IProjectLogic {
    bool TargetsOldFramework(IProject project);
    bool DoAllConfigurationsHaveNuspecs(IProject project);
}