using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleManager : MonoBehaviour
{
    private static AssetBundleManager instance;

    public static AssetBundleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("AssetBundleManager").AddComponent<AssetBundleManager>();
            }

            return instance;
        }
    }

    private Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, int> referenceCount = new Dictionary<string, int>();

    private HashSet<string> preservedAssetBundles = new HashSet<string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator LoadAssetBundle(string bundlePath)
    {
        // 检查是否已经加载了该AssetBundle
        if (loadedAssetBundles.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
            yield break;
        }

        // 发起UnityWebRequest加载AssetBundle
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
        yield return request.SendWebRequest();

        // 处理加载成功的情况
        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            loadedAssetBundles.Add(bundlePath, bundle);
            referenceCount.Add(bundlePath, preservedAssetBundles.Contains(bundlePath) ? 1 : 0);
            preservedAssetBundles.Remove(bundlePath);
        }
        // 处理加载失败的情况
        else
        {
            Debug.LogError($"Error loading asset bundle at path: {bundlePath}. Error: {request.error}");
        }
    }

    public T LoadAsset<T>(string bundlePath, string assetName) where T : Object
    {
        // 检查是否已经加载了该AssetBundle
        if (!loadedAssetBundles.ContainsKey(bundlePath))
        {
            Debug.LogError($"Asset bundle not loaded at path: {bundlePath}");
            return null;
        }

        // 从AssetBundle中加载资源
        AssetBundle bundle = loadedAssetBundles[bundlePath];
        T asset = bundle.LoadAsset<T>(assetName);

        // 检查是否加载成功
        if (asset == null)
        {
            Debug.LogError($"Error loading asset {assetName} from bundle {bundlePath}");
        }

        // 更新引用计数
        if (referenceCount.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
        }

        return asset;
    }

    public void UnloadAssetBundle(string bundlePath)
    {
        // 检查是否已经加载了该AssetBundle
        if (!loadedAssetBundles.ContainsKey(bundlePath))
        {
            Debug.LogError($"Asset bundle not loaded at path: {bundlePath}");
            return;
        }

        // 减少引用计数
        if (referenceCount.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]--;
        }

        // 如果引用计数为0且不在保留列表中，则卸载AssetBundle
        if (referenceCount[bundlePath] == 0 && !preservedAssetBundles.Contains(bundlePath))
        {
            loadedAssetBundles[bundlePath].Unload(true);
            loadedAssetBundles.Remove(bundlePath);
            referenceCount.Remove(bundlePath);
        }
    }

    public void PreserveAssetBundle(string bundlePath)
    {
        // 将AssetBundle加入保留列表中
        preservedAssetBundles.Add(bundlePath);

        // 如果AssetBundle已经加载，则将其引用计数加1
        if (loadedAssetBundles.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
        }
    }
}
//在需要加载资源的脚本中，调用 StartCoroutine(AssetBundleManager.Instance.LoadAssetBundle(bundlePath)) 加载指定路径的AssetBundle。
//在需要使用资源的脚本中，调用 AssetBundleManager.Instance.LoadAsset<T>(bundlePath, assetName) 加载指定名称的资源。
//在不需要使用AssetBundle的时候，可以调用 AssetBundleManager.Instance.UnloadAssetBundle(bundlePath) 卸载指定路径的AssetBundle。
