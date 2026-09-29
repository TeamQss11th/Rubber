using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace Rubber.EditorTools
{
    public static class BeachVillaPlayerBuild
    {
        [MenuItem("Rubber/Expansion/6 Build Validation Player")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Exit Play before building.");
            Directory.CreateDirectory("Builds/BeachVillaValidationPlayer");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Modern Villa/Scenes/Beach Villa.unity"},locationPathName="Builds/BeachVillaValidationPlayer/BeachVilla.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            File.WriteAllText("Docs/BeachVillaExpansion/build.txt",$"Result={report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; duration={report.summary.totalTime}; bytes={report.summary.totalSize}");
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Validation player build failed; inspect build report.");
        }
    }
}
