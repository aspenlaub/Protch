using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
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
        protected static TestTargetFolder PakledConsumerCoreTarget = new TestTargetFolder(nameof(ProjectFactoryTest), "PakledConsumerCore");
        protected static TestTargetFolder ChabStandardTarget = new TestTargetFolder(nameof(ProjectFactoryTest), "ChabStandard");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGittyAndPegh(new DummyCsArgumentPrompter()).UseGittyTestUtilities().UseProtch().Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(PakledConsumerCoreTarget);
            TargetInstaller.CreateCakeFolder(PakledConsumerCoreTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
            TargetInstaller.CreateCakeFolder(ChabStandardTarget, out errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            TargetInstaller.DeleteCakeFolder(PakledConsumerCoreTarget);
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
        }

        [TestInitialize]
        public void Initialize() {
            PakledConsumerCoreTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledConsumerCoreTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestMethod]
        public void CanLoadPakledConsumerCoreProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/PakledConsumerCore.git";
            gitUtilities.Clone(url, "master", PakledConsumerCoreTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            gitUtilities.Pull(PakledConsumerCoreTarget.Folder(), "UserName", "user.name@aspenlaub.org");

            var solutionFileFullName = PakledConsumerCoreTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerCoreTarget.SolutionId + ".sln";
            var projectFileFullName = PakledConsumerCoreTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerCoreTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            var sut = vContainer.Resolve<IProjectFactory>();
            var project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            var projectLogic = vContainer.Resolve<IProjectLogic>();
            Assert.IsTrue(projectLogic.IsANetStandardOrCoreProject(project));
            Assert.IsTrue(projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(project));
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual((object) PakledConsumerCoreTarget.SolutionId, project.ProjectName);
            Assert.AreEqual("netcoreapp3.0", project.TargetFramework);
            Assert.AreEqual(3, project.PropertyGroups.Count);
            Assert.AreEqual("git", project.RepositoryType);
            Assert.AreEqual(url, project.RepositoryUrl);
            Assert.AreEqual("master", project.RepositoryBranch);
            var rootNamespace = "";
            foreach (var propertyGroup in project.PropertyGroups) {
                Assert.IsNotNull(propertyGroup);
                Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
                if (propertyGroup.Condition == "") {
                    rootNamespace = propertyGroup.RootNamespace;
                    Assert.IsTrue(propertyGroup.AssemblyName.StartsWith("Aspenlaub.Net.GitHub.CSharp." + PakledConsumerCoreTarget.SolutionId), $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
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
                    Assert.AreEqual(propertyGroup.Condition.Contains("Debug|") ? "" : "PakledConsumerCore.nuspec", propertyGroup.NuspecFile);
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

            projectFileFullName = PakledConsumerCoreTarget.Folder().SubFolder("src").FullName + @"\Test\" + PakledConsumerCoreTarget.SolutionId + ".Test.csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual(PakledConsumerCoreTarget.SolutionId + ".Test", project.ProjectName);
        }

        [TestMethod]
        public void CanLoadChabStandardProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/ChabStandard.git";
            gitUtilities.Clone(url, "master", ChabStandardTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            gitUtilities.Pull(ChabStandardTarget.Folder(), "UserName", "user.name@aspenlaub.org");

            var solutionFileFullName = ChabStandardTarget.Folder().SubFolder("src").FullName + @"\" + ChabStandardTarget.SolutionId + ".sln";
            var projectFileFullName = ChabStandardTarget.Folder().SubFolder("src").FullName + @"\" + ChabStandardTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            var sut = vContainer.Resolve<IProjectFactory>();
            var project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            var projectLogic = vContainer.Resolve<IProjectLogic>();
            Assert.IsTrue(projectLogic.IsANetStandardOrCoreProject(project));
            Assert.IsTrue(projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(project));
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual((object) ChabStandardTarget.SolutionId, project.ProjectName);
            Assert.AreEqual("netstandard2.0", project.TargetFramework);
            Assert.AreEqual(3, project.PropertyGroups.Count);
            Assert.AreEqual("git", project.RepositoryType);
            Assert.AreEqual(url, project.RepositoryUrl);
            Assert.AreEqual("master", project.RepositoryBranch);
            var rootNamespace = "";
            foreach (var propertyGroup in project.PropertyGroups) {
                Assert.IsNotNull(propertyGroup);
                Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
                if (propertyGroup.Condition == "") {
                    rootNamespace = propertyGroup.RootNamespace;
                    Assert.IsTrue(propertyGroup.AssemblyName.StartsWith("Aspenlaub.Net.GitHub.CSharp." + ChabStandardTarget.SolutionId), $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
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
                        Assert.AreEqual("ChabStandard.nuspec", propertyGroup.NuspecFile);
                    }
                    Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                }
            }

            Assert.AreEqual(0, project.ReferencedDllFiles.Count);

            Assert.AreEqual(rootNamespace, project.RootNamespace);

            projectFileFullName = ChabStandardTarget.Folder().SubFolder("src").FullName + @"\Test\" + ChabStandardTarget.SolutionId + ".Test.csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual(ChabStandardTarget.SolutionId + ".Test", project.ProjectName);
        }
    }
}
