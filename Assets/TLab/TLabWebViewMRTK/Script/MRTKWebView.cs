using UnityEngine;
using TLab.Android.WebView;

namespace TLab.XR.MRTK
{
    public class MRTKWebView : MonoBehaviour
    {
        [SerializeField] private TLabWebView m_webview;
        [SerializeField] private GameObject m_keyborad;

        public void SwitchKeyboradActive()
        {
            m_keyborad.SetActive(!m_keyborad.activeSelf);
        }
    }
}
