using UnityEngine;
using TLab.VKeyborad;
using TLab.Android.WebView;

namespace TLab.XR.MRTK
{
    public class MRTKWebView : MonoBehaviour
    {
        [SerializeField] private TLabWebView m_webview;
        [SerializeField] private TLabVKeyborad m_keyborad;

        public void SwitchKeyboradActive() => m_keyborad.SetVisibility(!m_keyborad.isActive);
    }
}
