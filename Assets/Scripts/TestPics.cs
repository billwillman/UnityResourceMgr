using UnityEngine;
using System.Collections;

public class TestPics : MonoBehaviour {

    public TestDownPic[] m_Pics = null;

    public void OnBtnDownClick() {
		#if _USE_NGUI
        if (m_Pics != null && m_Pics.Length > 0) {
            for (int i = 0; i < m_Pics.Length; ++i) {
                TestDownPic pic = m_Pics[i];
                if (pic != null)
                    pic.StartHttp();
            }
        }
		#endif
    }
}
