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
        // ����Ƿ��Ѿ������˸�AssetBundle
        if (loadedAssetBundles.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
            yield break;
        }

        // ����UnityWebRequest����AssetBundle
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
        yield return request.SendWebRequest();

        // ������سɹ������
        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            loadedAssetBundles.Add(bundlePath, bundle);
            referenceCount.Add(bundlePath, preservedAssetBundles.Contains(bundlePath) ? 1 : 0);
            preservedAssetBundles.Remove(bundlePath);
        }
        // �������ʧ�ܵ����
        else
        {
            Debug.LogError($"Error loading asset bundle at path: {bundlePath}. Error: {request.error}");
        }
    }

    public T LoadAsset<T>(string bundlePath, string assetName) where T : Object
    {
        // ����Ƿ��Ѿ������˸�AssetBundle
        if (!loadedAssetBundles.ContainsKey(bundlePath))
        {
            Debug.LogError($"Asset bundle not loaded at path: {bundlePath}");
            return null;
        }

        // ��AssetBundle�м�����Դ
        AssetBundle bundle = loadedAssetBundles[bundlePath];
        T asset = bundle.LoadAsset<T>(assetName);

        // ����Ƿ���سɹ�
        if (asset == null)
        {
            Debug.LogError($"Error loading asset {assetName} from bundle {bundlePath}");
        }

        // �������ü���
        if (referenceCount.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
        }

        return asset;
    }

    public void UnloadAssetBundle(string bundlePath)
    {
        // ����Ƿ��Ѿ������˸�AssetBundle
        if (!loadedAssetBundles.ContainsKey(bundlePath))
        {
            Debug.LogError($"Asset bundle not loaded at path: {bundlePath}");
            return;
        }

        // �������ü���
        if (referenceCount.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]--;
        }

        // ������ü���Ϊ0�Ҳ��ڱ����б��У���ж��AssetBundle
        if (referenceCount[bundlePath] == 0 && !preservedAssetBundles.Contains(bundlePath))
        {
            loadedAssetBundles[bundlePath].Unload(true);
            loadedAssetBundles.Remove(bundlePath);
            referenceCount.Remove(bundlePath);
        }
    }

    public void PreserveAssetBundle(string bundlePath)
    {
        // ��AssetBundle���뱣���б���
        preservedAssetBundles.Add(bundlePath);

        // ���AssetBundle�Ѿ����أ��������ü�����1
        if (loadedAssetBundles.ContainsKey(bundlePath))
        {
            referenceCount[bundlePath]++;
        }
    }
}
//����Ҫ������Դ�Ľű��У����� StartCoroutine(AssetBundleManager.Instance.LoadAssetBundle(bundlePath)) ����ָ��·����AssetBundle��
//����Ҫʹ����Դ�Ľű��У����� AssetBundleManager.Instance.LoadAsset<T>(bundlePath, assetName) ����ָ�����Ƶ���Դ��
//�ڲ���Ҫʹ��AssetBundle��ʱ�򣬿��Ե��� AssetBundleManager.Instance.UnloadAssetBundle(bundlePath) ж��ָ��·����AssetBundle��
