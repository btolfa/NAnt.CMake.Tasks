using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.CMake.Tasks
{
    [TaskName("cmake-configure")]
    public class CMakeConfigureTask : ExternalProgramBase
    {
        #region Private Instance Fields

        private DirectoryInfo mBuildDirectory;
        private string mBuildType;
        private string mGenerator;
        private FileInfo mPreloadScript;
        private string mCmakePath = "cmake";
        private string mCmakeArgs;

        #endregion Private Instance Fields

        #region Public Instance Properties



        /// <summary>
        /// The source directory (relative to basedir). This is the directory that contains the top-level cmake script(<c>CMakeLists.txt</c>)
        /// </summary>
        /// <value>
        /// The directory that contains the top-level cmake script.
        /// </value>
        /// <remarks>
        /// <para>
        /// The source directory will be evaluated relative to the project's
        /// base directory if it is relative.
        /// </para>
        /// </remarks>
        [TaskAttribute("sourcedir", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public DirectoryInfo SourceDirectory { get; set; }

        /// <summary>
        /// The build directory (relative to basedir). The directory where the project will be build in. Will be created relative to basedir.
        /// </summary>
        /// <value>
        /// The directory where the project will be build in. 
        /// </value>
        /// <remarks>
        /// <para>
        /// The build directory will be evaluated relative to the project's
        /// base directory if it is relative.
        /// </para>
        /// </remarks>
        [TaskAttribute("builddir")]
        public DirectoryInfo BuildDirectory
        {
            get
            {
                if (mBuildDirectory == null)
                {
                    return base.BaseDirectory;
                }
                return mBuildDirectory;
            }
            set { mBuildDirectory = value; }
        }

        /// <summary>
        /// This statically specifies what build type (configuration) will be built in this build tree.
        /// Common values are &lt;empty>, Debug, Release, RelWithDebInfo and MinSizeRl
        /// </summary>
        /// <value>
        /// The desired build type (configuration). 
        /// </value>
        /// <remarks>
        /// <para>
        /// An empty value is usually used with multi configuration generators such as Visual Studio. 
        /// The Debug/Release values are 
        /// usually used with single configuration generators such as Ninja or Unix Makefiles.
        /// If the value is not empty, the CMake cache variable <c>CMAKE_BUILD_TYPE</c>
        /// will pre-populated with the value before CMake invocation.
        /// </para>
        /// </remarks>
        [TaskAttribute("buildtype")]
        public string BuildType
        {
            get { return mBuildType; }
            set { mBuildType = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// CMake´s <a href="http://www.cmake.org/cmake/help/latest/manual/cmake-generators.7.html#cmake-generators" target="_blank">buildscript generator</a>
        /// to use (e.g. <code>Unix Makefiles</code>). Leave empty to let CMake choose a generator.
        /// </summary>
        /// <value>
        /// The desired build script generator to use.
        /// </value>
        [TaskAttribute("generator")]
        public string Generator
        {
            get { return mGenerator; }
            set { mGenerator = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Optional path to a pre-load script file to populate the CMake cache.
        /// </summary>
        /// <remarks>
        /// If not empty, this value will be passed as the value of cmake`s <c>-C</c> commandline option.
        /// </remarks>
        [TaskAttribute("preloadscript")]
        public FileInfo PreloadScript
        {
            get { return mPreloadScript; }
            set { mPreloadScript = value; }
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
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElement("environment")]
        public EnvironmentSet EnvironmentSet { get; } = new EnvironmentSet();

        [TaskAttribute("cmakeargs")]
        public string CmakeArgs
        {
            get { return mCmakeArgs; }
            set { mCmakeArgs = StringUtils.ConvertEmptyToNull(value); }
        }


        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Perform additional checks after the task has been initialized.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            try
            {
                // just check if cmake path is a valid filename
                if (Path.IsPathRooted(CmakePath))
                {
                    // do nothing
                }
            }
            catch (Exception ex)
            {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' is not a valid value for attribute 'cmakepath' of <{1}>",
                    CmakePath, Name), Location, ex);
            }
        }

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

        public override string ProgramArguments {
            get
            {
                ArgumentCollection arguments = new ArgumentCollection();

                // Source directory
                arguments.Add(new Argument { Directory = SourceDirectory });

                // Preload script
                if (Generator != null)
                {
                    arguments.Add(new Argument("-C"));
                    arguments.Add(new Argument(mPreloadScript));
                }

                // Generator
                if (Generator != null)
                {
                    arguments.Add(new Argument("-G"));
                    arguments.Add(new Argument(mGenerator));
                }

                // Build type
                if (BuildType != null)
                {
                    arguments.Add(new Argument($"-DCMAKE_BUILD_TYPE={BuildType}"));
                }

                // CmakeArgs
                StringBuilder args = new StringBuilder(arguments.ToString());
                if (CmakeArgs != null)
                {
                    args.Append(' ');
                    args.Append(CmakeArgs);
                }

                return args.ToString();
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

        #endregion Override implementation of ExternalProgramBase

    }
}
