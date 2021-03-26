using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch {
    public class ProjectLogic : IProjectLogic {
        public bool IsANetStandardOrCoreProject(IProject project) {
            return project.TargetFramework.Contains("netstandard")
                   || project.TargetFramework.Contains("netcore")
                   || project.TargetFramework.Contains("net5");
        }

        public bool TargetsOldFramework(IProject project) {
            return project.TargetFramework.Contains("4.5") || project.TargetFramework.Contains("4.6");
        }

        public bool DoAllNetStandardOrCoreConfigurationsHaveNuspecs(IProject project) {
            return !IsANetStandardOrCoreProject(project)
                   || project.PropertyGroups.Where(propertyGroup
                           => propertyGroup.Condition.Contains("Release"))
                       .All(propertyGroup => propertyGroup.NuspecFile == project.ProjectName + ".nuspec");
        }
    }
}
