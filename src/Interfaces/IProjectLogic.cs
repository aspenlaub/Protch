// ReSharper disable UnusedMember.Global
namespace Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces {
    public interface IProjectLogic {
        bool IsANetStandardOrCoreProject(IProject project);
        bool TargetsOldFramework(IProject project);
        bool DoAllNetStandardOrCoreConfigurationsHaveNuspecs(IProject project);
    }
}
