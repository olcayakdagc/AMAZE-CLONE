using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T m_Instance = null;
    public static T instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;

                if (m_Instance == null)
                {

                    isTemporaryInstance = true;
                    m_Instance = new GameObject("Temp Instance of " + typeof(T).ToString(), typeof(T)).GetComponent<T>();

                  
                }
                if (!_isInitialized)
                {
                    _isInitialized = true;
                }
            }
            return m_Instance;
        }
    }

    public static bool isTemporaryInstance { private set; get; }

    private static bool _isInitialized;

  
    private void Awake()
    {
        if (m_Instance == null)
        {
            m_Instance = this as T;
        }
        else if (m_Instance != this)
        {
            DestroyImmediate(this);
            return;
        }
        if (!_isInitialized)
        {
            _isInitialized = true;
        }
    }


    private void OnApplicationQuit()
    {
        m_Instance = null;
    }
}