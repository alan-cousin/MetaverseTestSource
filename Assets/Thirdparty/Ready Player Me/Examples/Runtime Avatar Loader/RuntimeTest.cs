using System.Collections;
using UnityEngine;

namespace ReadyPlayerMe
{
    public class RuntimeTest : MonoBehaviour
    {
        private string AvatarURL = "";

        void Awake()
        {
            //Debug.Log("avataUrl:" + AvatarURL);
        }
        private void Start()
        {
          
        }
        public void GetMainPlayer(string avatarUrl)
        {
            AvatarURL = avatarUrl;
            AvatarLoader avatarLoader = new AvatarLoader();
            Debug.Log(AvatarURL);
            avatarLoader.LoadAvatar(AvatarURL, OnAvatarImported, OnAvatarLoaded);
        }

        private void OnAvatarImported(GameObject avatar)
        {
            Debug.Log("OnAvatarImported!");
        }
        private void OnAvatarLoaded(GameObject avatar, AvatarMetaData metaData)
        {
            Debug.Log("OnAvatarLoaded!");
            SendMessage("ReceivedAvata", avatar);
        }
    }
}
