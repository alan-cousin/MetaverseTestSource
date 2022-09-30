using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MusicOnOff : MonoBehaviour
{
    UnityEngine.UI.Toggle m_toggle;

    // Start is called before the first frame update
    void Start()
    {
        m_toggle = gameObject.GetComponent<Toggle>();

        if (MusicSetting.Instance != null)
            m_toggle.isOn = MusicSetting.Instance.MusicOnOff;
        else
        {
            m_toggle.isOn = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //setting music on/off
    public void OnChangeState()
    {
        MusicSetting.Instance.MusicOnOff = m_toggle.isOn;
        Debug.Log("MusicState");

        if (m_toggle.isOn)
        {
            ChatManager.Instance.PlayVoiceChat();
        }
        else
        {
            ChatManager.Instance.StopVoiceChat();
        }
    }
}
