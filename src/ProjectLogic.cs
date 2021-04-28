using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch {
    public class ProjectLogic : IProjectLogic {
        public bool TargetsOldFramework(IProject project) {
            return project.TargetFramework.Contains("4.")
                || project.TargetFramework.Contains("netstandard")
                || project.TargetFramework.Contains("netcore");
        }

        public bool DoAllConfigurationsHaveNuspecs(IProject project) {
            return project.PropertyGroups.Where(propertyGroup => propertyGroup.Condition.Contains("Release"))
                .All(propertyGroup => propertyGroup.NuspecFile == project.ProjectName + ".nuspec");
        }
    }
}
