using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class Root : MonoBehaviour
{
    /// <summary>
    /// 是否是编辑器模式
    /// </summary>
    public bool editor;
    public string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
    public string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";

    private async void Start()
    {
        //初始化 YooAsset
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        var package = YooAssets.CreatePackage("DefaultPackage");
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

        //2 设置游戏运行模式 (在线/编辑器)
        await SimulateMode(package);

        //3 获取资源版本
        var packageVersion = await UpdatePackageVersion(package);

        //4 更新对应版本的资源清单文件
        await UpdatePackageManifest(package,packageVersion);

        //5 下载游戏资源
        await Download(package);
        

        //7 补充元数据
        foreach (var assetInfo in package.GetAssetInfos("metaData"))
        {
            AssetHandle handle = package.LoadAssetAsync<TextAsset>(assetInfo.AssetPath);
            await handle.Task;
            TextAsset textAsset = handle.AssetObject as TextAsset;
            if (textAsset == null) continue;
            byte[] dllBytes = textAsset.bytes;
            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
        }

        //8 华佗LoadDll
        foreach (var assetInfo in package.GetAssetInfos("hotUpdateDll"))
        {
            AssetHandle handle = package.LoadAssetAsync<TextAsset>(assetInfo.AssetPath);
            await handle.Task;
            TextAsset textAsset = handle.AssetObject as TextAsset;
            if (textAsset == null) continue;
            byte[] dllBytes = textAsset.bytes;
            Assembly.Load(dllBytes);
        }
        
        //9 加载结束 运行游戏
        string location = "Assets/GameRes/Scene/scene1";
        var sceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single;
        bool suspendLoad = false;
        SceneHandle sceneHandle = package.LoadSceneAsync(location, sceneMode, suspendLoad);
        await sceneHandle.Task;
    }
    
    /// <summary>
    /// 2 设置运行模式
    /// </summary>
    /// <param name="package"></param>
    private async Task SimulateMode(ResourcePackage package)
    {
        if (editor)
        {
            var initParameters = new EditorSimulateModeParameters();
            var simulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, "DefaultPackage");
            initParameters.SimulateManifestFilePath  = simulateManifestFilePath;
            await package.InitializeAsync(initParameters).Task;
        }
        else
        {
            // 注意：GameQueryServices.cs 太空战机的脚本类，详细见StreamingAssetsHelper.cs
            var initParameters = new HostPlayModeParameters();
            initParameters.BuildinQueryServices = new GameQueryServices(); 
            initParameters.DecryptionServices = new FileOffsetDecryption();
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var initOperation = package.InitializeAsync(initParameters);
            await initOperation.Task;
    
            if(initOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("资源包初始化成功！");
            }
            else 
            {
                Debug.LogError($"资源包初始化失败：{initOperation.Error}");
            }
        }
    }
    
    //3 更新资源版本
    private async Task<string> UpdatePackageVersion(ResourcePackage package)
    {
        var operation = package.UpdatePackageVersionAsync();
        await operation.Task;
        if (operation.Status == EOperationStatus.Succeed)
        {
            //更新成功
            var packageVersion = operation.PackageVersion;
            Debug.Log($"Updated package Version : {packageVersion}");
            return packageVersion;
        }
        else
        {
            //更新失败
            Debug.LogError(operation.Error);
            return "";
        }
    }
    
    //4 更新manifest文件
    private async Task UpdatePackageManifest(ResourcePackage package, string packageVersion)
    {
        // 更新成功后自动保存版本号，作为下次初始化的版本。
        // 也可以通过operation.SavePackageVersion()方法保存。
        var operation = package.UpdatePackageManifestAsync(packageVersion, true);
        await operation.Task;

        if (operation.Status == EOperationStatus.Succeed)
        {
            //更新成功
        }
        else
        {
            //更新失败
            Debug.LogError(operation.Error);
        }
    }
    
    /// <summary>
    /// 5 下载资源
    /// </summary>
    /// <returns>下载是否成功</returns>
    private async Task<bool> Download(ResourcePackage package,int downloadingMaxNum = 10,int failedTryAgain = 3)
    {
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
    
        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("没有需要下载的资源");
            return true;
        }

        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;    

        //注册回调方法
        // downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
        // downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
        // downloader.OnDownloadOverCallback = OnDownloadOverFunction;
        // downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

        //开启下载
        downloader.BeginDownload();
        await downloader.Task;

        //检测下载结果
        return downloader.Status == EOperationStatus.Succeed;
    }
    
    
    
}
