using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Protch.Test {
    [TestClass]
    public class ProjectFactoryTest {
        protected static TestTargetFolder PakledConsumerTarget = new(nameof(ProjectFactoryTest), "PakledConsumer");
        protected static TestTargetFolder ChabTarget = new(nameof(ProjectFactoryTest), "Chab");
        private static IContainer Container;
        protected static ITestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _) {
            Container = new ContainerBuilder().UseGittyAndPegh(new DummyCsArgumentPrompter()).UseGittyTestUtilities().UseProtch().Build();
            TargetRunner = Container.Resolve<ITestTargetRunner>();
        }

        [TestInitialize]
        public void Initialize() {
            PakledConsumerTarget.Delete();
            ChabTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledConsumerTarget.Delete();
            ChabTarget.Delete();
        }

        [TestMethod]
        public void CanLoadPakledConsumerProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/PakledConsumer.git";
            gitUtilities.Clone(url, "master", PakledConsumerTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            gitUtilities.Pull(PakledConsumerTarget.Folder(), "UserName", "user.name@aspenlaub.org");

            var solutionFileFullName = PakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerTarget.SolutionId + ".sln";
            var projectFileFullName = PakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            var sut = Container.Resolve<IProjectFactory>();
            var project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            var projectLogic = Container.Resolve<IProjectLogic>();
            Assert.IsTrue(!projectLogic.TargetsOldFramework(project));
            Assert.IsTrue(projectLogic.DoAllConfigurationsHaveNuspecs(project));
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual((object) PakledConsumerTarget.SolutionId, project.ProjectName);
            Assert.AreEqual("net5.0", project.TargetFramework);
            Assert.AreEqual(3, project.PropertyGroups.Count);
            Assert.AreEqual("git", project.RepositoryType);
            Assert.AreEqual(url, project.RepositoryUrl);
            Assert.AreEqual("master", project.RepositoryBranch);
            Assert.AreEqual("PakledConsumer", project.PackageId);
            var rootNamespace = "";
            foreach (var propertyGroup in project.PropertyGroups) {
                Assert.IsNotNull(propertyGroup);
                Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
                if (propertyGroup.Condition == "") {
                    rootNamespace = propertyGroup.RootNamespace;
                    Assert.IsTrue(propertyGroup.AssemblyName.StartsWith("Aspenlaub.Net.GitHub.CSharp." + PakledConsumerTarget.SolutionId), $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
                    Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                    Assert.AreEqual("false", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.IntermediateOutputPath);
                    Assert.AreEqual("", propertyGroup.OutputPath);
                    Assert.AreEqual("false", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.NuspecFile);
                    Assert.AreEqual("false", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                } else {
                    Assert.AreEqual("", propertyGroup.AssemblyName);
                    Assert.AreEqual(propertyGroup.Condition.Contains("Debug|") ? "" : "PakledConsumer.nuspec", propertyGroup.NuspecFile);
                    Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                    Assert.AreEqual("", propertyGroup.OutputPath);
                    Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                }
            }

            Assert.AreEqual(0, project.ReferencedDllFiles.Count);

            Assert.AreEqual(rootNamespace, project.RootNamespace);

            projectFileFullName = PakledConsumerTarget.Folder().SubFolder("src").FullName + @"\Test\" + PakledConsumerTarget.SolutionId + ".Test.csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual(PakledConsumerTarget.SolutionId + ".Test", project.ProjectName);
        }

        [TestMethod]
        public void CanLoadChabProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/Chab.git";
            gitUtilities.Clone(url, "master", ChabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            gitUtilities.Pull(ChabTarget.Folder(), "UserName", "user.name@aspenlaub.org");

            var solutionFileFullName = ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".sln";
            var projectFileFullName = ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            var sut = Container.Resolve<IProjectFactory>();
            var project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            var projectLogic = Container.Resolve<IProjectLogic>();
            Assert.IsTrue(!projectLogic.TargetsOldFramework(project));
            Assert.IsTrue(projectLogic.DoAllConfigurationsHaveNuspecs(project));
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual((object) ChabTarget.SolutionId, project.ProjectName);
            Assert.AreEqual("net5.0", project.TargetFramework);
            Assert.AreEqual(3, project.PropertyGroups.Count);
            Assert.AreEqual("git", project.RepositoryType);
            Assert.AreEqual(url, project.RepositoryUrl);
            Assert.AreEqual("master", project.RepositoryBranch);
            Assert.AreEqual("Chab", project.PackageId);
            var rootNamespace = "";
            foreach (var propertyGroup in project.PropertyGroups) {
                Assert.IsNotNull(propertyGroup);
                Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
                if (propertyGroup.Condition == "") {
                    rootNamespace = propertyGroup.RootNamespace;
                    Assert.IsTrue(propertyGroup.AssemblyName.StartsWith("Aspenlaub.Net.GitHub.CSharp." + ChabTarget.SolutionId), $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
                    Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                    Assert.AreEqual("false", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.IntermediateOutputPath);
                    Assert.AreEqual("", propertyGroup.OutputPath);
                    Assert.AreEqual("false", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.NuspecFile);
                    Assert.AreEqual("false", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                } else {
                    Assert.AreEqual("", propertyGroup.AssemblyName);
                    if (propertyGroup.Condition.Contains("Debug|")) {
                        Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                        Assert.AreEqual("", propertyGroup.OutputPath);
                        Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                        Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                        Assert.AreEqual("", propertyGroup.NuspecFile);
                    } else {
                        Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                        Assert.AreEqual("", propertyGroup.OutputPath);
                        Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                        Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                        Assert.AreEqual("Chab.nuspec", propertyGroup.NuspecFile);
                    }
                    Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                }
            }

            Assert.AreEqual(0, project.ReferencedDllFiles.Count);

            Assert.AreEqual(rootNamespace, project.RootNamespace);

            projectFileFullName = ChabTarget.Folder().SubFolder("src").FullName + @"\Test\" + ChabTarget.SolutionId + ".Test.csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual(ChabTarget.SolutionId + ".Test", project.ProjectName);
        }
    }
}
