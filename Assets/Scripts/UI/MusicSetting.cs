using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MusicSetting : MonoBehaviour
{
    public static MusicSetting Instance { get; private set; }

    private bool m_musicOnOff;
    private void Awake()
    {
        Instance = this;
    }
    public bool MusicOnOff { get {
            return m_musicOnOff;
        }
        set {
            m_musicOnOff= value;
            SaveSetting();
        }
    }
  
    public void SaveSetting()
    {
        PlayerPrefs.SetInt("MusicOnOff", MusicOnOff ? 1 : 0);
        PlayerPrefs.Save();
    }
}
