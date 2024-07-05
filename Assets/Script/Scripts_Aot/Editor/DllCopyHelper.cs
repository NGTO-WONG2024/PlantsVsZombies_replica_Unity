using System.IO;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace Script.Scripts_Aot.Editor
{
    public class DllCopyHelper
    {
        static readonly string platform = (EditorUserBuildSettings.activeBuildTarget).ToString();
        static readonly string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

        private static string MetaDataFolder =>
            Path.Combine(projectPath, "HybridCLRData", "AssembliesPostIl2CppStrip", platform);

        private static string HotUpdateFolder => Path.Combine(projectPath, "HybridCLRData", "HotUpdateDlls", platform);

        [MenuItem("Assets/CopyDll2")]
        public static void CopyDll2()
        {
        }

        [MenuItem("Assets/CopyDll")]
        public static void CopyDll()
        {
            
            foreach (var item in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                var filePath = Path.Combine(HotUpdateFolder, item.name+".dll");
                var folder = Path.Combine(Application.dataPath, "GameRes", "Dll", "HotUpdate");
                CopyDllAndRename(filePath, folder);
            }
            foreach (var item in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                var filePath = Path.Combine(MetaDataFolder, item);
                var folder = Path.Combine(Application.dataPath, "GameRes", "Dll", "Metadata");
                CopyDllAndRename(filePath, folder);
            }
            
        }

        private static void CopyDllAndRename(string dllFilePath, string targetFolder)
        {
            File.Copy(dllFilePath, Path.Combine(targetFolder, Path.GetFileName(dllFilePath) + ".bytes"), true);
        }
        
    }
}