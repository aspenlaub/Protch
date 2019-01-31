using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
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
        protected static TestTargetFolder PakledConsumerTarget = new TestTargetFolder(nameof(ProjectFactoryTest), "PakledConsumer");
        protected static TestTargetFolder ChabStandardTarget = new TestTargetFolder(nameof(ProjectFactoryTest), "ChabStandard");
        private static IContainer vContainer;
        protected static TestTargetInstaller TargetInstaller;
        protected static TestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            vContainer = new ContainerBuilder().UseGitty().UseGittyTestUtilities().UseProtch().Build();
            TargetInstaller = vContainer.Resolve<TestTargetInstaller>();
            TargetRunner = vContainer.Resolve<TestTargetRunner>();
            TargetInstaller.DeleteCakeFolder(PakledConsumerTarget);
            TargetInstaller.CreateCakeFolder(PakledConsumerTarget, out var errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
            TargetInstaller.CreateCakeFolder(ChabStandardTarget, out errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            TargetInstaller.DeleteCakeFolder(PakledConsumerTarget);
            TargetInstaller.DeleteCakeFolder(ChabStandardTarget);
        }

        [TestInitialize]
        public void Initialize() {
            PakledConsumerTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledConsumerTarget.Delete();
            ChabStandardTarget.Delete();
        }

        [TestMethod]
        public void CanLoadPakledConsumerProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/PakledConsumer.git";
            gitUtilities.Clone(url, PakledConsumerTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().IgnoreOutdatedBuildCakePendingChangesAndDoNotPush(PakledConsumerTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            var solutionFileFullName = PakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerTarget.SolutionId + ".sln";
            var projectFileFullName = PakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + PakledConsumerTarget.SolutionId + ".csproj";
            Assert.IsTrue(File.Exists(projectFileFullName));
            var sut = vContainer.Resolve<IProjectFactory>();
            var project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
            var projectLogic = vContainer.Resolve<IProjectLogic>();
            Assert.IsFalse(projectLogic.IsANetStandardOrCoreProject(project));
            Assert.IsTrue(projectLogic.DoAllNetStandardOrCoreConfigurationsHaveNuspecs(project));
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsNotNull(project);
            Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
            Assert.AreEqual((object) PakledConsumerTarget.SolutionId, project.ProjectName);
            Assert.AreEqual("v4.6", project.TargetFramework);
            Assert.AreEqual(3, project.PropertyGroups.Count);
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
                    Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.NuspecFile);
                    Assert.AreEqual("", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                } else {
                    Assert.AreEqual("", propertyGroup.AssemblyName);
                    if (propertyGroup.Condition.Contains("Debug|")) {
                        Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                        Assert.AreEqual(@"bin\Debug\", propertyGroup.OutputPath);
                    } else {
                        Assert.AreEqual("false", propertyGroup.UseVsHostingProcess);
                        Assert.AreEqual(@"bin\Release\", propertyGroup.OutputPath);
                    }
                    Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                    Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.NuspecFile);
                    Assert.AreEqual("", propertyGroup.Deterministic);
                    Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
                }
            }

            Assert.AreEqual(2, project.ReferencedDllFiles.Count);
            // Assert.IsTrue(project.ReferencedDllFiles.All(f => File.Exists(f)), "File/-s not found:\r\n" + string.Join("\r\n", project.ReferencedDllFiles.Where(f => !File.Exists(f))));
            Assert.IsTrue(project.ReferencedDllFiles.Any(f => f.EndsWith(".Pakled.dll")));
            Assert.IsTrue(project.ReferencedDllFiles.Any(f => f.EndsWith(".Json.dll")));

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
        public void CanLoadChabStandardProject() {
            var gitUtilities = new GitUtilities();
            var errorsAndInfos = new ErrorsAndInfos();
            const string url = "https://github.com/aspenlaub/ChabStandard.git";
            gitUtilities.Clone(url, ChabStandardTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            vContainer.Resolve<TestTargetRunner>().IgnoreOutdatedBuildCakePendingChangesAndDoNotPush(ChabStandardTarget, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

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
                    Assert.AreEqual("false", propertyGroup.GenerateAssemblyInfo);
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
