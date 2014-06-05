using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace IsobarExtension.Actions
{
    [ActionProperties(
        "Configuration Transforms",
        "Automatically run configuration transformations on all *.config files")]
    [Tag(Tags.Isobar)]
    [Tag(Inedo.BuildMaster.Tags.ConfigurationFiles)]
    [CustomEditor(typeof(XmlConfigTransformActionEditor))]
    public class XmlConfigTransformAction : AgentBasedActionBase
    {
        #region Public Properties
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
        #endregion

        #region Public Methods
        protected override void Execute()
        {
            // see http://inedo.com/support/kb/1070/writing-an-agent-based-action for the list
            // of services provided by an agent
            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();

            LogInformation("Looking for any configuration transform files...");

            // Get all the config files within the application
            var configFiles = EnumerateFilesRecursively(Context.SourceDirectory, new string[] { "*.config" });

            LogInformation(String.Format("Found {0} config file(s)", configFiles.Count()));

            foreach (var configSourceFile in configFiles)
            {
                var alreadyRun = new HashSet<string>();

                // Apply any transforms where the suffix is Release.config
                ApplyConfigTransforms(configSourceFile, "Release", fileOps, alreadyRun);

                if (!String.IsNullOrWhiteSpace(EnvironmentSuffix))
                {
                    // Apply any transforms where the suffic is {EnvironmentSuffix}.config
                    ApplyConfigTransforms(configSourceFile, EnvironmentSuffix, fileOps, alreadyRun);
                }

                // Apply any addition transforms
                foreach (var suffix in GetSuffixes(AdditionalTransforms))
                {
                    ApplyConfigTransforms(configSourceFile, suffix, fileOps, alreadyRun);
                }
            }
        }

        public override string ToString()
        {
            return "Apply config transforms on remote agent.";
        }
        #endregion

        #region Private Methods
        private void ApplyConfigTransforms(string sourceFile, string suffix, IFileOperationsExecuter fileOps,
                                          HashSet<string> alreadyRun)
        {
            if (!suffix.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
            {
                suffix = suffix + ".config";
            }

            // Update the extension of the found config to the suffix extensions
            string transformFile = Path.ChangeExtension(sourceFile, suffix);

            // Try and find the transform config to apply to the source file
            if ((fileOps.FileExists(transformFile) &&
                 !string.Equals(sourceFile, transformFile, StringComparison.InvariantCultureIgnoreCase)) &&
                !alreadyRun.Contains(transformFile))
            {
                alreadyRun.Add(transformFile);

                var transformExePath = Path.Combine(fileOps.GetBaseWorkingDirectory(),
                                                    @"ExtTemp\WindowsSDK\Resources\ctt.exe");

                if (!fileOps.FileExists(transformExePath))
                    throw new FileNotFoundException("ctt.exe could not be found on the agent.", transformExePath);

                // Strip out the source directory location so that our BuildArguments is directory independent
                var relativeSourceFilePath = sourceFile.Replace(Context.SourceDirectory, "");
                var relativeTransformFilePath = transformFile.Replace(Context.SourceDirectory, "");

                // Get all the arguments that form the executable transform task
                var arguments = BuildArguments(relativeSourceFilePath, relativeTransformFilePath);

                LogInformation("Performing XDT transform...");

                // Call the transform executable for the file
                ExecuteCommandLine(transformExePath, arguments);
            }
        }

        /// <summary>
        /// Build a list of command line arguments for config tranformation task
        /// </summary>
        /// <param name="sourceFile">The source file to be transformed</param>
        /// <param name="transformFile">The transform file to be applied onto the source file</param>
        /// <returns>An argument string</returns>
        private string BuildArguments(string sourceFile, string transformFile)
        {
            LogDebug("Target Directory: " + Context.TargetDirectory);

            var buffer = new StringBuilder();
            buffer.AppendFormat("source:\"{0}\"", Path.Combine(Context.SourceDirectory, sourceFile));
            buffer.AppendFormat(" transform:\"{0}\"", Path.Combine(Context.SourceDirectory, transformFile));
            buffer.AppendFormat(" destination:\"{0}\"", Path.Combine(Context.TargetDirectory, sourceFile));
            buffer.Append(" indent");

            if (PreserveWhitespace)
                buffer.AppendFormat(" preservewhitespace");
            if (Verbose)
                buffer.Append(" verbose");

            return buffer.ToString();
        }

        /// <summary>
        /// Recursively search through a directory or directories for files based on a search pattern
        /// </summary>
        /// <param name="parentDirectoryPath">The starting directory to recursively search</param>
        /// <param name="searchPatterns">The search pattern to be applied</param>
        /// <returns>An enumerable list of filenames</returns>
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

            // If no search pattern is defined, then return all files
            return Directory.EnumerateFiles(parentDirectoryPath, "*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Get all individual suffixes from a list of comma delimited suffixes
        /// </summary>
        /// <param name="suffixes">The list of suffixes to split</param>
        /// <returns>A string list of suffixes</returns>
        private IEnumerable<string> GetSuffixes(string suffixes)
        {
            if (String.IsNullOrWhiteSpace(suffixes))
            {
                return new string[0];
            }

            // Split the suffix variable by comma and return as an array
            return
                (from s in suffixes.Split(new char[] { ',' })
                 select s.Trim() into s
                 where s.Length > 0
                 select s)
                    .ToArray<string>();
        }
        #endregion
    }
}
