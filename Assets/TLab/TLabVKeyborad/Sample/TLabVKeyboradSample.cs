using UnityEngine;
using TMPro;

namespace TLab.InputField
{
    public class TLabVKeyboradSample : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI isThisMobile;

#if !UNITY_EDITOR && UNITY_WEBGL
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool IsMobile();
#endif

        void Start()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
        isThisMobile.text = IsMobile() ? "Mobile" : "PC";
        return;
#endif

#if UNITY_ANDROID
        isThisMobile.text = "Mobile";
        return;
#endif
            isThisMobile.text = "PC";
        }
    }
}
