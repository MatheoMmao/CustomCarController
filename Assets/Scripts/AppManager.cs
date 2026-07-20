using System;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Events;

public enum AppMode
{
    PCMode,
    VRMode
}

public class AppManager : MonoBehaviour
{
    static AppManager instance;
    public static AppManager Instance
    {
        get
        {
            if (instance != null)
                return instance;
            else
            {
                GameObject go = new GameObject();
                go.AddComponent<AppManager>();
                return instance;
            }
        }
    }

    AppMode mode;

    public UnityEvent onVRMode;
    public UnityEvent onPCMode;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetPCMode();
    }

    [ContextMenu("Set VR Mode")]
    public void SetVRMode() => VerifyAppMode(AppMode.VRMode);
    
    [ContextMenu("Set PC Mode")]
    public void SetPCMode() => VerifyAppMode(AppMode.PCMode);

    public void VerifyAppMode(AppMode target)
    {
        if (UnityEngine.XR.XRSettings.enabled && target == AppMode.VRMode)
        {
            Debug.Log("VR Mode");
            mode = AppMode.VRMode;

            onVRMode?.Invoke();
        }
        else
        {
            Debug.Log("PC Mode");

            if (target==AppMode.VRMode)
            {
                Debug.Log("Couldn't find the VR headset, cannot switch to VR");
            }
            mode = AppMode.PCMode;

            onPCMode?.Invoke();
        }
    }
}
