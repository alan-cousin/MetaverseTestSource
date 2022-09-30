using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PACToggle : MonoBehaviour
{
    Image m_Background;
    Toggle m_toggle;
    // Start is called before the first frame update
    void Start()
    {
        m_toggle = gameObject.GetComponent<Toggle>();
        m_Background = m_toggle.targetGraphic.GetComponent<Image>();
        //m_mark = toggle_comp.graphic.GetComponent<Image>();

    }

    // Update is called once per frame
    void Update()
    {
        m_Background.enabled = !m_toggle.isOn;
    }
}
