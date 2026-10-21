using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public enum AppMode
{
    PCMode,
    VRMode
}

public class AppManager : MonoBehaviour
{
    private static AppManager instance;
    public static AppManager Instance
    {
        get
        {
            if (instance != null)
                return instance;
            else
            {
                GameObject go = new GameObject("AppManager");
                instance = go.AddComponent<AppManager>();
                return instance;
            }
        }
    }

    private AppMode mode;

    public UnityEvent onVRMode;
    public UnityEvent onPCMode;

    private InputDevice headDevice;
    private bool lastPresenceState = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        FetchHeadsetDevice();

        SetPCMode();
    }

    void Update()
    {
        if (!headDevice.isValid)
        {
            FetchHeadsetDevice();
            return;
        }

        bool isUserPresent = IsUserWearingHeadset();

        if (isUserPresent != lastPresenceState)
        {
            lastPresenceState = isUserPresent;
            OnPresenceChanged(isUserPresent);
        }
    }

    private bool IsUserWearingHeadset()
    {
        if (!headDevice.isValid) return false;

        if (headDevice.TryGetFeatureValue(CommonUsages.userPresence, out bool userPresent))
        {
            return userPresent;
        }

        return false;
    }

    [ContextMenu("Set VR Mode")]
    public void SetVRMode() => VerifyAppMode(AppMode.VRMode);

    [ContextMenu("Set PC Mode")]
    public void SetPCMode() => VerifyAppMode(AppMode.PCMode);

    public void VerifyAppMode(AppMode target)
    {
        // Modern XR Check replacing legacy XRSettings.enabled
        bool isXrLoaderActive = XRGeneralSettings.Instance != null &&
                                XRGeneralSettings.Instance.Manager != null &&
                                XRGeneralSettings.Instance.Manager.activeLoader != null;

        if (isXrLoaderActive && target == AppMode.VRMode)
        {
            Debug.Log("VR Mode");
            mode = AppMode.VRMode;
            onVRMode?.Invoke();
        }
        else
        {
            Debug.Log("PC Mode");
            if (target == AppMode.VRMode)
            {
                Debug.Log("Couldn't find the VR headset, cannot switch to VR");
            }
            mode = AppMode.PCMode;
            onPCMode?.Invoke();
        }
    }

    private void FetchHeadsetDevice()
    {
        var hmds = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmds);

        if (hmds.Count > 0)
        {
            headDevice = hmds[0];
        }
    }

    private void OnPresenceChanged(bool isWearing)
    {
        if (isWearing)
        {
            mode = AppMode.VRMode;
            onVRMode?.Invoke();
        }
        else
        {
            mode = AppMode.PCMode;
            onPCMode?.Invoke();
        }
    }
}