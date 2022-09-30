using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AllArt.Solana.Nft;
public class NFTFrame : MonoBehaviour
{
    public string NFTAddress = "";
    public UnityEvent OnClick = new UnityEvent();
    public GameObject frame_obj;

   // public GameObject nft_selection;
    // Start is called before the first frame update
    void Start()
    {
        SetNFT(NFTAddress);
    }

    // Update is called once per frame
    void Update()
    {
        frame_obj.gameObject.SetActive(NFTAddress != "");
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit Hit;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out Hit) && Hit.collider.gameObject == gameObject)
            {
                Debug.Log("Button Clicked");
                OnClick.Invoke();
                GameObject.Find("uBuildManager").GetComponent<uBuildManager>().showNFTExplorer(true, gameObject);
            }
        }
    }

    public void SetNFT(string f_nft_address)
    {
        if (f_nft_address == "") return;
        NFTAddress = f_nft_address;
        Nft nft_data = Nft.TryLoadNftFromLocal(NFTAddress);
        if (nft_data != null)
        {
            MeshRenderer _renderer = frame_obj.GetComponent<MeshRenderer>();

            if (_renderer == null) return;

            Material[] _mat_array = _renderer.materials;
            if(_mat_array.Length > 0 && _mat_array[0] != null)
            {
                _mat_array[0].SetTexture("_BaseMap", nft_data.metaplexData.nftImage.file);
               
            }
        }
    }
}
