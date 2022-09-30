using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AllArt.Solana.Nft;
using Solnet.Rpc.Models;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using UnityEngine.Events;
public class NFTItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private GameObject _loadingIcon;
    [SerializeField]
    private Image _image;
    private bool _isInitialized;
    public bool _isLoading;
    private Nft _nft;
    public UnityEvent OnItemClick = new UnityEvent();
    [SerializeField]
    public GameObject nftPrefab;

    // Start is called before the first frame update
    void Start()
    {
        _isInitialized = false;
        _isLoading = false;
        gameObject.AddComponent<Outline>();
        GetComponent<Outline>().effectColor = Color.cyan;
        GetComponent<Outline>().effectDistance = new Vector2(3, -3);
        GetComponent<Outline>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        _image.gameObject.SetActive(_isInitialized);
        _loadingIcon.SetActive(!_isInitialized);
        if (!_isInitialized && !_isLoading)
        {
            LoadingNFT();
        }
        if (isExit && !_isInitialized) gameObject.SetActive(false); 
    }
    float delayTime = 0;
    bool isExit = false;
    void FixedUpdate()
    {
        if (isExit) return;
        delayTime += Time.deltaTime;
        if(delayTime > 30)
        {
            delayTime = 0;
            isExit = true;
        }
    }

    public async void LoadingNFT()
    {
        if (m_tokenInfo == null)
            return;
        if (float.Parse(m_tokenInfo.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)
        {
            _isLoading = true;
            while (_isLoading)
            {
                try
                {
                    Nft nft = await Nft.TryGetNftData(m_tokenInfo.Account.Data.Parsed.Info.Mint, SolWeb3Connector.instance.activeRpcClient, true);
                    _nft = nft;
                    if (nft != null)
                    {
                        _image.sprite = Sprite.Create(nft.metaplexData.nftImage.file, new Rect(0, 0, nft.metaplexData.nftImage.file.width, nft.metaplexData.nftImage.file.height), new Vector2(0.5f, 0.5f));

                        NftPiece piece = new NftPiece();
                        piece.m_nft = _nft;
                        piece.prefab = nftPrefab;
                        piece.tokenKey = m_tokenInfo.Account.Data.Parsed.Info.Mint;
                        NftManager.Instance.pieces.Add(piece);
                        Debug.Log("Add pieces:" + NftManager.Instance.pieces.Count);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }

                    _isLoading = false;

                    _isInitialized = true;
                }
                catch (Exception e)
                {
                    await Task.Delay(3000);
                    Debug.Log(e.Message);
                    gameObject.SetActive(false);
                }
            }
        }
    }

    TokenAccount m_tokenInfo;
    public void InitializeData(TokenAccount f_account, Nft f_nft)
    {
        m_tokenInfo = f_account;
        _nft = f_nft;
        _isInitialized = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isLoading || !_isInitialized)
            return;

        int index = 0;

        for (int i = 0; i < this.gameObject.transform.parent.childCount; i++)
        {
            gameObject.transform.parent.GetChild(i).gameObject.GetComponent<Outline>().enabled = false;
        }

        foreach (NftPiece piece in NftManager.Instance.pieces)
        {
            if (_nft == piece.m_nft)
            {
                NftManager.Instance.selectPiece(index);
                GetComponent<Outline>().enabled = true;
            }
            index++;
        }
    }
}
