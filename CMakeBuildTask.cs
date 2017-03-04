using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.CMake.Tasks
{
    [TaskName("cmake-build")]
    public class CMakeBuildTask : ExternalProgramBase
    {
        private string mCmakePath = "cmake";
        private DirectoryInfo mBuildDirectory;
        private string mTarget;
        private string mConfiguration;
        private bool mCleanFirst = false;
        private string mOptionsForNativeTool;

        /// <summary>
        /// Project binary directory to be built.
        /// </summary>
        [TaskAttribute("builddir")]
        public DirectoryInfo BuildDirectory
        {
            get
            {
                return mBuildDirectory ?? base.BaseDirectory;
            }
            set { mBuildDirectory = value; }
        }

        /// <summary>
        /// For multi-configuration tools, choose configuration.
        /// </summary>
        [TaskAttribute("config")]
        public string Configuration
        {
            get { return mConfiguration; }
            set { mConfiguration = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Path to cmake execute 
        /// </summary>
        [TaskAttribute("cmakepath")]
        [StringValidator(AllowEmpty = false)]
        public string CmakePath
        {
            get { return mCmakePath; }
            set { mCmakePath = value; }
        }

        /// <summary>
        /// Build <c>target</c> instead of default targets. May only be specified once.
        /// </summary>
        [TaskAttribute("target")]
        public string Target
        {
            get { return mTarget; }
            set { mTarget = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Build target clean first, then build. (To clean only, use target clean.)
        /// </summary>
        [TaskAttribute("cleanfirst")]
        [BooleanValidator]
        public bool CleanFirst
        {
            get { return mCleanFirst; }
            set { mCleanFirst = value; }
        }

        /// <summary>
        /// Pass remaining options to the native tool.
        /// </summary>
        [TaskAttribute("options-for-native-tool")]
        public string OptionsForNativeTool
        {
            get { return mOptionsForNativeTool; }
            set { mOptionsForNativeTool = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElement("environment")]
        public EnvironmentSet EnvironmentSet { get; } = new EnvironmentSet();

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        public override string ProgramFileName
        {
            get
            {
                if (Path.IsPathRooted(CmakePath))
                {
                    return CmakePath;
                }
                // resolve program to full path relative to project directory
                string fullPath = Project.GetFullPath(CmakePath);
                // check if the program exists in that location
                if (File.Exists(fullPath))
                {
                    // return full path to program (which we know exists)
                    return fullPath;
                }
                return CmakePath;
            }
        }

        public override string ProgramArguments
        {
            get
            {
                ArgumentCollection arguments = new ArgumentCollection();

                // Build directory
                arguments.Add(new Argument("--build"));
                arguments.Add(new Argument { Directory = BuildDirectory });

                // Target
                if (Target != null)
                {
                    arguments.Add(new Argument("--target"));
                    arguments.Add(new Argument(Target));
                }

                // Configuration
                if (Configuration != null)
                {
                    arguments.Add(new Argument("--config"));
                    arguments.Add(new Argument(Configuration));
                }

                // Clean First
                if (CleanFirst)
                {
                    arguments.Add(new Argument("--clean-first"));
                }

                if (OptionsForNativeTool != null)
                {
                    arguments.Add(new Argument {Line = OptionsForNativeTool});
                }

                return arguments.ToString();
            }
        }

        protected override void PrepareProcess(Process process)
        {
            base.PrepareProcess(process);

            // set working directory to build directory
            process.StartInfo.WorkingDirectory = BuildDirectory.FullName;

            // set environment variables
            foreach (EnvironmentVariable variable in EnvironmentSet.EnvironmentVariables)
            {
                if (variable.IfDefined && !variable.UnlessDefined)
                {
                    if (variable.Value == null)
                    {
                        process.StartInfo.EnvironmentVariables[variable.VariableName] = "";
                    }
                    else
                    {
                        process.StartInfo.EnvironmentVariables[variable.VariableName] = variable.Value;
                    }
                }
            }
        }

    }
}
