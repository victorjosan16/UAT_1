#!/usr/bin/env bash
# Compiles VoxelBloom.Core, VoxelBloom.Runtime and the EditMode tests with Roslyn against Unity reference assemblies,
# to catch C# errors before a Unity Cloud Build. Needs mono plus, in $REF (default /tmp/claude-0/ref):
#   roslyn/  = NuGet microsoft.net.compilers 4.2.0      mods/ = NuGet unityengine.modules 2021.3.33
#   ui/      = NuGet unity3d.unityengine.ui 2020.3.21    nunit/ = NuGet nunit 3.13.3
# The reference assemblies are older than Unity 6, so Unity 6-only APIs (e.g. Rigidbody.linearVelocity) are reported
# and can be ignored. Editor scripts are not checked (UnityEditor.dll is not on NuGet).
REF=${REF:-/tmp/claude-0/ref}; cd "$(dirname "$0")/../Assets/VoxelBloom" || exit 1
CSC="mono $REF/roslyn/tools/csc.exe -nologo -langversion:9.0 -nowarn:0618,0414,0649,0169,0219,0067 -r:/usr/lib/mono/4.5/Facades/netstandard.dll"
UNITY=$(ls $REF/mods/lib/net45/*.dll | grep -v "UnityEngine.dll$" | sed 's/^/-r:/' | tr '\n' ' ')
$CSC -target:library -out:/tmp/vb_core.dll $(find Scripts/Core -name "*.cs") | grep error
$CSC -target:library -out:/tmp/vb_rt.dll -r:/tmp/vb_core.dll $UNITY -r:$REF/ui/lib/UnityEngine.UI.dll $(find Scripts -name "*.cs" -not -path "*/Core/*") | grep error
$CSC -target:library -out:/tmp/vb_tests.dll -r:/tmp/vb_core.dll $UNITY -r:$REF/nunit/lib/net45/nunit.framework.dll $(find Tests/EditMode -name "*.cs") | grep error
echo "check done"
