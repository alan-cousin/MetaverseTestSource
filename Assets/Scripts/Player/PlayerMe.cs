using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMe : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject player = null;
    public GameObject avatarBg;

    private GameObject _loadingCover = null;
    private GameObject _welcomeDialog = null;
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
    private void Update()
    {
        if (PlayerPrefs.GetInt("loading") == 1 && player == null)
        {
            player = GameObject.FindGameObjectWithTag("ReadyAvatar").transform.GetChild(0).GetChild(0).gameObject; 
            player.transform.SetParent(transform);
            _loadingCover = GameObject.Find("LoadingCover");
            if (_loadingCover != null) _loadingCover.SetActive(false);
            _welcomeDialog = GameObject.Find("Welcome");
            if (_welcomeDialog != null) _welcomeDialog.SetActive(false);
            avatarBg.SetActive(true);
        }
    }
}
