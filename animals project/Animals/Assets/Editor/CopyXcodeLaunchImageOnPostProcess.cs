using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class CopyXcodeLaunchImageOnPostProcess
{
    const string XCODE_IMAGES_FOLDER = "Unity-iPhone/Images.xcassets";
    const string SOURCE_FOLDER_NAME = "Splash.imageset";
    const string SOURCE_FOLDER_ROOT = "Xcode Storyboard/splash/splash/Assets.xcassets";
    
    [PostProcessBuildAttribute(1)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        Debug.Log("[OnPostprocessBuild] run");

        if(buildTarget == BuildTarget.iOS)
        {
            string sourcePath = $"{SOURCE_FOLDER_ROOT}/{SOURCE_FOLDER_NAME}";
            string targetPath = $"{path}/{XCODE_IMAGES_FOLDER}/{SOURCE_FOLDER_NAME}";

            FileUtil.DeleteFileOrDirectory(targetPath);
            FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
        }
    }
}
