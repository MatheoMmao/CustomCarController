using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class RecenterPlayer : MonoBehaviour
{
    [SerializeField] XROrigin m_XROrigin;
    [SerializeField] private Transform m_point;

    [SerializeField] InputActionReference m_actionReference;

    private void Start()
    {
        //Recenter();

        m_actionReference.action.Enable();
        m_actionReference.action.performed += (_) => Recenter();
    }

    public void Recenter()
    {
        m_XROrigin.MoveCameraToWorldLocation(m_point.position);

        m_XROrigin.MatchOriginUpCameraForward(m_point.up, m_point.forward);
    }


    public void DelayRecenter()
    {
        StartCoroutine(RecenterCoroutine());
    }

    public IEnumerator RecenterCoroutine()
    {
        yield return null;

        Recenter();
    }
}
