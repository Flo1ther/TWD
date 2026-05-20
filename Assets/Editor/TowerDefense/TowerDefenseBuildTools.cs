using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TowerDefenseBuildTools
{
    private const string BuildPath = "Builds/WebGL";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tower Defense/Configure WebGL Build")]
    public static void ConfigureWebGlBuild()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
        };

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "Tower Defense";
        PlayerSettings.companyName = "Student Project";
        AssetDatabase.SaveAssets();
        Debug.Log("Tower Defense WebGL build settings configured.");
    }

    [MenuItem("Tower Defense/Build WebGL")]
    public static void BuildWebGl()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("WebGL build postponed: Unity is still compiling scripts or importing assets. Wait until the editor finishes, then run Tower Defense > Build WebGL again.");
            return;
        }

        ConfigureWebGlBuild();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"WebGL build succeeded: {BuildPath}");
        else
            Debug.LogError($"WebGL build failed: {report.summary.result}. Check the first error above this message in the Console.");
    }

    [MenuItem("Tower Defense/Build WebGL", true)]
    private static bool CanBuildWebGl()
    {
        return !EditorApplication.isCompiling && !EditorApplication.isUpdating;
    }
}
