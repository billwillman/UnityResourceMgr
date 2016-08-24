using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    public class CachedMonoBehaviour : MonoBehaviour
    {

        public GameObject CachedGameObject
        {
            get
            {
                if (m_CachedGameObj == null)
                    m_CachedGameObj = this.gameObject;
                return m_CachedGameObj;
            }
        }

        public Transform CachedTransform
        {
            get
            {
                return GetCachedComponent<Transform>();
            }
        }

        public T GetCachedComponent<T>() where T : UnityEngine.Component
        {
            System.Type t = typeof(T);
            UnityEngine.Component ret;
            if (m_CachedCompentMap == null || !m_CachedCompentMap.TryGetValue(t, out ret))
            {
                GameObject gameObj = CachedGameObject;
                if (gameObj == null)
                    return null;
				
				if (m_CahcedCompentInitMap != null) {
					if (m_CahcedCompentInitMap.Contains (t))
						return null;
				}

				if (m_CahcedCompentInitMap == null)
					m_CahcedCompentInitMap = new HashSet<System.Type> ();
				m_CahcedCompentInitMap.Add (t);

                T target = gameObj.GetComponent<T>();
                if (target == null)
                    return null;
                CheckCachedCompentMap();
                m_CachedCompentMap.Add(t, target);
                return target;
            }

            T comp = ret as T;
            return comp;
        }

        private void CheckCachedCompentMap()
        {
            if (m_CachedCompentMap == null)
                m_CachedCompentMap = new Dictionary<System.Type, Component>();
        }


        private Dictionary<System.Type, UnityEngine.Component> m_CachedCompentMap = null;
		private HashSet<System.Type> m_CahcedCompentInitMap = null;
        private GameObject m_CachedGameObj = null;
    }
}
