using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;

namespace IsobarExtension.Actions
{
    [ActionProperties(
        "Configuration Transforms",
        "Automatically run configuration transformation files")]
    [Tag("sample")]
    public class XmlConfigTransformAction : AgentBasedActionBase
    {
        [Persistent]
        public string EnvironmentSuffix { get; set; }

        [Persistent]
        public string AdditionalTransforms { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to preserve or collapse whitespace.
        /// </summary>
        [Persistent]
        public bool PreserveWhitespace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether verbose logging should be enabled.
        /// </summary>
        [Persistent]
        public bool Verbose { get; set; }

        protected override void Execute()
        {
            // see http://inedo.com/support/kb/1070/writing-an-agent-based-action for the list
            // of services provided by an agent
            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();

            var configFiles = EnumerateFilesRecursively(Context.SourceDirectory, new string[] { "*.config" });

            foreach (var configSourceFile in configFiles)
            {
                var alreadyRun = new HashSet<string>();

                ApplyConfigTransform(configSourceFile, "Release", fileOps, alreadyRun);
            }
        }

        public override string ToString()
        {
            return "Writes all variables to variables.txt in the default directory on the remote agent.";
        }

        private void ApplyConfigTransform(string sourceFile, string suffix, IFileOperationsExecuter fileOps,
                                          HashSet<string> alreadyRun)
        {
            if (!suffix.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
            {
                suffix = suffix + ".config";
            }

            string transformFile = Path.ChangeExtension(sourceFile, suffix);

            if ((fileOps.FileExists(transformFile) &&
                 !string.Equals(sourceFile, transformFile, StringComparison.InvariantCultureIgnoreCase)) &&
                !alreadyRun.Contains(transformFile))
            {
                alreadyRun.Add(transformFile);

                var transformExePath = Path.Combine(fileOps.GetBaseWorkingDirectory(),
                                                    @"ExtTemp\WindowsSDK\Resources\ctt.exe");

                if(!fileOps.FileExists(transformExePath))
                    throw new FileNotFoundException("ctt.exe could not be found on the agent.", transformExePath);

                var arguments = BuildArguments(sourceFile, transformFile);

                LogInformation("Performing XDT transform...");

                ExecuteCommandLine(transformExePath, arguments);
            }
        }

        private string BuildArguments(string sourceFile, string transformFile)
        {
            var buffer = new StringBuilder();
            buffer.AppendFormat("source:\"{0}\"", Path.Combine(Context.SourceDirectory, sourceFile));
            buffer.AppendFormat(" transform:\"{0}\"", Path.Combine(Context.SourceDirectory, transformFile));
            buffer.AppendFormat(" destination:\"{0}\"", Path.Combine(Context.SourceDirectory, sourceFile));
            buffer.Append(" indent");

            if (PreserveWhitespace)
                buffer.AppendFormat(" preservewhitespace");
            if (Verbose)
                buffer.Append(" verbose");

            return buffer.ToString();
        }

        private IEnumerable<string> EnumerateFilesRecursively(string parentDirectoryPath, params string[] searchPatterns)
        {
            var list = new List<string>();

            if (searchPatterns.Length != 0)
            {
                foreach (var pattern in searchPatterns)
                {
                    list.AddRange(Directory.EnumerateFiles(parentDirectoryPath, pattern, SearchOption.AllDirectories));
                }

                return list;
            }
            return Directory.EnumerateFiles(parentDirectoryPath, "*", SearchOption.AllDirectories);
        }
    }
}
