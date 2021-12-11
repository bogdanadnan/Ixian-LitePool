#!/bin/sh -e
echo Rebuilding Ixian Lite Pool...
echo Cleaning previous build
msbuild IxianLitePool.sln /p:Configuration=Release /target:Clean
echo Removing packages
rm -rf packages
echo Restoring packages
nuget restore IxianLitePool.sln
echo Building Ixian Lite Pool
msbuild IxianLitePool.sln /p:Configuration=Release
echo Done rebuilding Ixian Lite Pool