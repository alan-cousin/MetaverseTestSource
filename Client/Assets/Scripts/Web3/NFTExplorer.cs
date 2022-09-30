using Solnet.Rpc.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using System;
using AllArt.Solana.Nft;
public class NFTExplorer : MonoBehaviour
{
    [SerializeField]
    private string updateAuthority;

    public Slider _slider;

    public NFTItem _itemPrefab;

    public GameObject _loadingPanel;

    private GameObject _notify;
    // Start is called before the first frame update
    void Start()
    {
        if (_loadingPanel == null) return;
        token_items = new List<NFTItem>();
        _notify = transform.Find("notify").gameObject;
        _notify.SetActive(false);
        GetOwnedTokenAccounts();
    }

    private void OnEnable()
    {
        ScrollRect scroll_rect = gameObject.GetComponent<ScrollRect>();
        scroll_rect.onValueChanged.AddListener(UpdateSliderValue);
    }

    private void OnDisable()
    {
        ScrollRect scroll_rect = gameObject.GetComponent<ScrollRect>();
        _slider.onValueChanged.RemoveListener(UpdateScrollPosition);
        scroll_rect.onValueChanged.RemoveListener(UpdateSliderValue);
    }
    private void UpdateSliderValue(Vector2 scrollPosition)
    {
        // Again, flippin the value for visual consistency
        _slider.SetValueWithoutNotify(1 - scrollPosition.y);
    }
    private void UpdateScrollPosition(float value)
    {
        ScrollRect scroll_rect = gameObject.GetComponent<ScrollRect>();
        scroll_rect.verticalNormalizedPosition = 1 - _slider.value;
    }
    // Update is called once per frame
    void Update()
    {

    }

    public List<NFTItem> token_items;
    void DisableTokenItems()
    {
        foreach (NFTItem token in token_items)
        {
            token.gameObject.SetActive(false);
        }
    }

    public NFTItem NewItem()
    {
        NFTItem nft_item = GameObject.Instantiate(_itemPrefab).GetComponent<NFTItem>();
        ScrollRect scroll_rect = gameObject.GetComponent<ScrollRect>();
        nft_item.transform.SetParent(scroll_rect.content.transform, false);
        return nft_item;
    }

    //get all NFTs of owner
    public async void GetOwnedTokenAccounts()
    {
        if (SolWeb3Connector.sInstance == null || SolWeb3Connector.sInstance.WalletAddress == "") return;
        _loadingPanel.SetActive(true);
        DisableTokenItems();
        bool _b_get_tokens = false;
        int itemCount = 0;
        while (!_b_get_tokens)
        {
            try
            {
                TokenAccount[] result = await SolWeb3Connector.instance.GetOwnedTokenAccounts(SolWeb3Connector.sInstance.WalletAddress);
                if (result != null && result.Length > 0)
                {
                    int itemIndex = 0;
                    itemCount = result.Length;
                    foreach (TokenAccount item in result)
                    {
                        if (float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) <= 0) continue;

                        Nft nft;
                        try
                        {
                            nft = await Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SolWeb3Connector.instance.activeRpcClient, true);

                            NFTItem list_item = null;
                            if (itemIndex >= token_items.Count)
                            {
                                list_item = NewItem();
                                token_items.Add(list_item);
                            }
                            else
                            {
                                list_item = token_items[itemIndex];
                            }
                            list_item.InitializeData(item, nft);
                            list_item.LoadingNFT();

                            list_item.gameObject.SetActive(true);
                            itemIndex++;
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                }
                if (result != null)
                {
                    if (result.Length == 0) _notify.SetActive(true);
                    _b_get_tokens = true;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                await Task.Delay(3000);
            }
        }
        _loadingPanel.SetActive(false);
    }

}
