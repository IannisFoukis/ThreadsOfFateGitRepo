using UnityEngine;

public class UIRoot : MonoBehaviour
{
    static UIRoot instance;

    public static UIRoot Instance => instance;

    [SerializeField] KeeperPronouncement keeperPronouncement;
    public KeeperPronouncement KeeperPronouncement => keeperPronouncement;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
