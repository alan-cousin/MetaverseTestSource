using System.Collections;
using System.Collections.Generic;
using LucidSightTools;
using TMPro;
using UnityEngine;
using Invector.vCharacterController;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : ExampleNetworkedEntityView
{
    private Quaternion _cachedHeadRotation;

    [SerializeField]
    private Transform _headRoot = null;
    [SerializeField]
    private GameObject _loadingCover;
    [SerializeField]
    private GameObject _welcomeDialog;

    public string userName;

    public bool isReady = false;

    public bool isCloned = false;

    public Vector3 preVelocity = Vector3.zero;
 

    protected override void Start()
    {
        base.Start();
        userName = string.Empty;
        StartCoroutine("WaitForConnect");
    }

    //if user joined in a room, create a new entity of user
    private IEnumerator WaitForConnect()
    {
        if (ExampleManager.Instance.CurrentUser != null && !IsMine)
        {
            yield break;
        }

        while (!ExampleManager.Instance.IsInRoom)
        {
            yield return new WaitForSeconds(1f);
        }

        if (isCloned == true) yield break;
        LSLog.LogImportant("HAS JOINED ROOM - CREATING ENTITY");
        ExampleManager.CreateNetworkedEntityWithTransform(transform.position, Quaternion.identity,
            new Dictionary<string, object> { ["userName"] = ExampleManager.Instance.UserName }, this, entity =>
            {
                userName = ExampleManager.Instance.UserName;
                ExampleManager.Instance.CurrentNetworkedEntity = entity;
            });
    }

   //remove entity of a user when user removed
    public override void OnEntityRemoved()
    {
        base.OnEntityRemoved();
        LSLog.LogImportant("REMOVING ENTITY", LSLog.LogColor.lime);
        Destroy(gameObject);
    }
    //set newly all datas for multi-communitcation
    protected override void ProcessViewSync()
    {
        // This is the target playback time of this body
        double interpolationTime = ExampleManager.Instance.GetServerTime - interpolationBackTimeMs;
        // Use interpolation if the target playback time is present in the buffer

        if (proxyStates[0].timestamp > interpolationTime)
        {
            // The longer the time since last update add a delta factor to the lerp speed to get there quicker
            float deltaFactor = ExampleManager.Instance.GetServerTimeSeconds > proxyStates[0].timestamp
                ? (float)(ExampleManager.Instance.GetServerTimeSeconds - proxyStates[0].timestamp) * 0.2f
                : 0f;

            if (syncLocalPosition)
            {
                myTransform.localPosition = Vector3.Slerp(myTransform.localPosition, proxyStates[0].pos,
                    Time.deltaTime * (positionLerpSpeed + deltaFactor));
            }

            if (syncLocalRotation && Mathf.Abs(Quaternion.Angle(transform.localRotation, proxyStates[0].rot)) >
                snapIfAngleIsGreater)
            {
                myTransform.localRotation = proxyStates[0].rot;
            }

            if (syncLocalRotation)
            {
                myTransform.localRotation = Quaternion.Slerp(myTransform.localRotation, proxyStates[0].rot,
                    Time.deltaTime * (rotationLerpSpeed + deltaFactor));
                _headRoot.localRotation = Quaternion.Slerp(_headRoot.localRotation, _cachedHeadRotation,
                    Time.deltaTime * (rotationLerpSpeed + deltaFactor));
            }
        }
        // Use extrapolation (If we didnt get a packet in the last X ms and object had velocity)
        else
        {
            EntityState latest = proxyStates[0];

            float extrapolationLength = (float)(interpolationTime - latest.timestamp) / 1000f;
            // Don't extrapolation for more than 500 ms, you would need to do that carefully
            if (extrapolationLength < extrapolationLimitMs / 1000f)
            {
                if (syncLocalPosition)
                {
                    myTransform.localPosition = latest.pos + latest.vel * extrapolationLength;
                }

                if (syncLocalRotation)
                {
                    myTransform.localRotation = latest.rot;
                }
            }
            else
            {
                myTransform.position = new Vector3((float)state.xPos, (float)state.yPos, (float)state.zPos);
                myTransform.rotation = new Quaternion((float)state.xRot, (float)state.yRot, (float)state.zRot, (float)state.wRot);
            }
        }

        float velLen = new Vector2(state.xVel, state.zVel).magnitude / Time.deltaTime;
        GetComponent<Animator>().SetFloat("InputMagnitude", Mathf.Clamp(velLen, 0f, 0.5f));
    }
    //update state of game
    protected override void UpdateViewFromState()
    {
        base.UpdateViewFromState();

        //catch the incoming head rotation. If it has xView, it will have the rest

        if (state.attributes.ContainsKey("xViewRot"))
        {
            _cachedHeadRotation.x = float.Parse(state.attributes["xViewRot"]);
            _cachedHeadRotation.y = float.Parse(state.attributes["yViewRot"]);
            _cachedHeadRotation.z = float.Parse(state.attributes["zViewRot"]);
            _cachedHeadRotation.w = float.Parse(state.attributes["wViewRot"]);
        }

        if (string.IsNullOrEmpty(userName) && state.attributes.ContainsKey("userName"))
        {
            userName = state.attributes["userName"];
        }

        if (state.attributes.ContainsKey("isReady"))
        {
            isReady = bool.Parse(state.attributes["isReady"]);
        }
    }

    protected override void UpdateStateFromView()
    {
        base.UpdateStateFromView();

        //Update the head rotation attributes
        
        Dictionary<string, string> updatedAttributes = new Dictionary<string, string>
        {
            {"xViewRot", _headRoot.localRotation.x.ToString()},
            {"yViewRot", _headRoot.localRotation.y.ToString()},
            {"zViewRot", _headRoot.localRotation.z.ToString()},
            {"wViewRot", _headRoot.localRotation.w.ToString()}
        };
        SetAttributes(updatedAttributes);

    }

    protected override void Update()
    {
        if (IsMine && !GetComponent<Animator>().enabled)
        {
            GetComponent<Animator>().enabled = true;
            _loadingCover.SetActive(false);
            _welcomeDialog.SetActive(true);
            GalleryGameManager.Instance.playable = true;
        }
        base.Update();
        
        if(!IsMine)
        {
            if((syncLocalPosition || syncLocalRotation) && GalleryGameManager.Instance.playable)
            {
                ProcessViewSync();
            }
        }
    }
}