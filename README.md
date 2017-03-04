# NAnt.CMake.Tasks

The nant tasks for building with [cmake](https://cmake.org)

## How to use

### By adding NAnt.CMake.Tasks.dll

You can place the compiled version of NAnt.CMake.Tasks.dll into the nant installation folder and start using cmake-configure and cmake-build tasks right after it without any additional changes.

## `<cmake-configure>` task

Execute cmake to configure project.

Use of nested `arg` element(s) is advised over the `cmakeargs` parameter, as it supports automatic quoting and can resolve relative to absolute paths.

### Parameters

Attribute|Type|Description|Required
---------|----|-----------|--------
sourcedir|directory|The directory that contatins the top-level cmake script (CMakeLists.txt)|true
builddir|directory|The directory where the project will be build in|false
buildtype|string|The desired build type (configuration)|false
generator|string|CMake's build script generator. Leave empty to let CMake choose a generator|false
preloadscript|path|Optional path to a pre-load script file to populate the CMake cache|false
cmakepath|string|Path to cmake executable. By default is "cmake"|false
cmakeargs|string|The command-line arguments for the cmake. These will be passed as is to the cmake. When quoting is necessary, these must be explictly set as part of the value. Consider using nested `arg` elements instead.|false


### Nested elements

Nested elements are the same as stadard [`<exec>` task](http://nant.sourceforge.net/release/0.92/help/tasks/exec.html) - `<envionment>` and `<arg>`

### Examples

```xml
<cmake-configure sourcedir="${root} builddir="${root}/build" />
```

```xml
<cmake-configure sourcedir="${root} builddir="${root}/build" generator="Ninja" buildtype="Debug">
    <arg value="-DBUILD_TESTING=FALSE" />
    <arg value="-DCMAKE_CXX_COMPILER_LAUNCHER=ccache" />
    <arg value="-DCMAKE_CXX_INCLUDE_WHAT_YOU_USE=include-what-you-use" />
</cmake-configure>
```

## `<cmake-build>` task

Execute cmake --build

### Parameters

Attribute|Type|Description|Required
---------|----|-----------|--------
builddir|directory|The directory where the project will be build in|false
config|string|The choosen configuration for multi-configuration tools|false
cmakepath|string|Path to cmake executable. By default is "cmake"|false
target|It will build this target instead of default targets|false
cleanfirst|bool|Build target clean first, then build|fasle
options-for-native-tool|string|This options will be passed to the native tool|false

### Nested elements

Nested elements are the same as stadard [`<exec>` task](http://nant.sourceforge.net/release/0.92/help/tasks/exec.html) - `<envionment>` and `<arg>`

### Examples

```xml
<cmake-build builddir="${root}/build" config="Debug" />
```

```xml
<cmake-build builddir="${root}/build" target="tests" >
    <arg value="-j8"/>
    <arg value="VERBOSE=1"/>
</cmake-build>
```
