using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager instance;
    public static ResourceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("ResourceManager").AddComponent<ResourceManager>();
            }
            return instance;
        }
    }

    private Dictionary<string, Object> loadedResources = new Dictionary<string, Object>();
    private Dictionary<string, int> resourceReferenceCount = new Dictionary<string, int>();

    public T LoadResource<T>(string path, bool cache = false, bool isAsync = false, System.Action<Object> onComplete = null) where T : Object
    {
        T resource = null;
        if (cache && loadedResources.ContainsKey(path))
        {
            resource = loadedResources[path] as T;
            IncreaseReferenceCount(path);
        }
        else
        {
            if (isAsync)
            {
                StartCoroutine(LoadAsync<T>(path, onComplete));
            }
            else
            {
                resource = Resources.Load<T>(path);
                if (cache && resource != null)
                {
                    loadedResources[path] = resource;
                    IncreaseReferenceCount(path);
                }
            }
        }
        return resource;
    }

    IEnumerator LoadAsync<T>(string path, System.Action<Object> onComplete) where T : Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);
        yield return request;
        if (request.asset != null)
        {
            if (onComplete != null)
            {
                onComplete(request.asset);
            }
        }
    }

    private void IncreaseReferenceCount(string path)
    {
        if (resourceReferenceCount.ContainsKey(path))
        {
            resourceReferenceCount[path]++;
        }
        else
        {
            resourceReferenceCount[path] = 1;
        }
    }

    private void DecreaseReferenceCount(string path)
    {
        if (resourceReferenceCount.ContainsKey(path))
        {
            resourceReferenceCount[path]--;
            if (resourceReferenceCount[path] == 0)
            {
                UnloadResource(path);
            }
        }
    }

    private void UnloadResource(string path)
    {
        if (loadedResources.ContainsKey(path))
        {
            Resources.UnloadAsset(loadedResources[path]);
            loadedResources.Remove(path);
            resourceReferenceCount.Remove(path);
        }
    }

    public void ReleaseResource(string path)
    {
        DecreaseReferenceCount(path);
    }

    public void CancelLoading()
    {
        StopAllCoroutines();
    }

    void OnDestroy()
    {
        foreach (string path in loadedResources.Keys)
        {
            Resources.UnloadAsset(loadedResources[path]);
        }
        loadedResources.Clear();
        resourceReferenceCount.Clear();
        instance = null;
    }
}
