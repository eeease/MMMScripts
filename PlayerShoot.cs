using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using InControl;
using MultiplayerWithBindingsExample;

public class PlayerShoot : MonoBehaviour
{
    public int playerNum, teamNum;

    public List<PlayerShoot> otherPlayers = new List<PlayerShoot>(); //for laneinfohelpers rn
    public LaneInfoHelper myLaneInfo;
    public string myPIN;
    public Button pinButton;
    public GameObject gunTip;
    public float damage = 5f;
    public float sniperD = 10f;
    public float maxShootDist = 15f;
    public Material defaultMat;

    LineRenderer lineRend;
    public ParticleSystem muzzleFlash, sniperJuiceParts;

    bool isClicking = false;
    public bool isSwapping;
    public bool isReady; //this is being set from playerjoinpanel.cs
    public bool waitingForSac;


    [Header("Rotation Vars")]
    public bool isInverted;
    public float xRot;
    public float xRotZ; //z = zoom.
    public float yRot;
    public float yRotZ;
    float swapXRot, swapYRot;
    private float xRotD = 5;
    public float xRotZD = 2.5f; //default Zoom x rotation
    private float yRotD = 5; //default values (user can change local vals in game) //!no support for saving atm.
    public float yRotZD = 2.5f; //default values (user can change local vals in game) //!no support for saving atm
    private float xMRotD = 2; //m = mouse
    private float yMRotD = 2;
    public float minX = -90;
    public float maxX = 90;
    private Quaternion charTarget;
    private Quaternion camTargetRot;
    public UnityStandardAssets.Characters.FirstPerson.MouseLook mouseControl; //to be set from fps
    public float spinSpeed = 10f;
    public bool quickRotate = true;
    public bool rotting;
    bool kickback;
    public float kickbackTime = .1f;
    public float kickbackAmount = 15f;

    //in-game options:
    public bool rumbleOn = true;
    public bool isLefty = false;
    public bool swapAssist = true;

    private Gem firstSetGem, secondSetGem;

    public GameObject gemSwapLR;
    LRFollowObj gsLR;
    public InputDevice Device { get; set; }
    public PlayerActions Actions;
    public Board myBoard;
    bool attackWarning = true;

    [Header("World UI")]
    public Canvas myWorldCan;
    public Image sJuiceFill;
    public float sJuice, sJuiceOG, juiceMod, juiceFill;
    public Image[] tmbj = new Image[4];
    public int laneIndex = 0;
    public int laneSelectIndex = 0;
    public Image gunEmote;
    public Sprite[] emoteSprites;
    //options panel:
    public GameObject optionsPane, itemPane;
    public Slider aimSlider, zoomSlider;
    public Button firstItem;
    public bool inOptions, inShop;
    public MyEventSystem m_events;
    public GameObject notifPrefab;
    public Animator warningIMG;
    public GameObject myEventCard;
    //item/shop stuff:
    public Image shopNotif, shopControl;
    public Sprite[] shopReg, shopAvail, shopCancel; //0 is going to be kbam for these
    GameObject lastSelected;
    public List<string> whiteRetTags;

    public List<Item> possibleItems = new List<Item>();
    public List<Item> purchasedItems = new List<Item>();
    //public LayerMask camLayerMask;

    public Item cheapestItem;
    //event card parts:
    public Image icon;
    public Image main, helper;
    public Text cName, effect, cdTimer;
    Sprite def_icon, def_main, def_helper;
    string def_name, def_effect;

    [Header("Gunz")]
    public LayerMask sniperMask;
    public Image reticle;
    public Transform dummyHand; //used to switch lefty/righty
    public GameObject[] guns; //0 = gem blaster; 1 = sniper rifle
    public int equippedGun;
    public float zoomAmt;
    private float zoomOG;
    public Camera myCam;
    public Animator gunAnim;
    public GameObject zoomIMG; //sniper zoom overlay
    bool canSnipe = true;
    [Tooltip(".4 is about Widowmaker zoom cooldown")]
    public float snipeCD;
    public int killsThisRound;
    public bool infiniteJuice, gemBuffHints;
    public AudioClip[] playerNoises;
    public AudioSource refAudio;
    //item one time abilities:
    public bool slowRounds, healingRounds, dotRounds, minionAutoSac;
    public GameObject gunTrailDad, gunTrailChild;//this will spawn where the ray hits and a particle or trail will go to it.
    public GameObject enemyHitParts;
    public Vector3 centerTrailOffset;
    public Material shootMat, swapMat;
    public ParticleSystem gTipParts;
    float refTargetX; //the initial xpos of the gun.
    Coroutine refCR;
    Gem prevGem, currGem;
    public float zoomCD = .5f;
    bool canZoom = true; //when clicking the right stick, we need a delay so it doesn't unzoom then zoom.

    public int chosenHat;

    float resetTime = 4;
    float resetOG = 4;
    float cinematicCD = 2f;
    float cinematicCDOG = 2f;



    void Awake()
    {
        lineRend = gunTip.GetComponent<LineRenderer>();
        charTarget = transform.parent.localRotation;

        camTargetRot = transform.localRotation;

    }

    private void Start()
    {
        canZoom = true;
        //lazy but testing:
        if (playerNum != 0)
        {
            teamNum = 1;
        }
        myCam = GetComponent<Camera>();
        zoomOG = myCam.fieldOfView;
        optionsPane.GetComponentInChildren<UIScrollToSelectionXY>().events = m_events;

        //boards are already there, so players should find all boards and assign themselves based on team num:
        //board team num set in inspector.  Player team num set a few lines up.
        //playerNum set when player is spawned (in PlayerManager.cs).
        Board[] bs = FindObjectsOfType<Board>();
        for (int i = 0; i < bs.Length; i++)
        {
            if (bs[i].teamNum == teamNum)
            {
                bs[i].myPlayer = this;
                myBoard = bs[i];

            }
        }
        //look at the 'center' of the board
        LookAtMiddleOfBoard();
        //initialize rotation so you start by looking at the board?
        Init(transform.parent, transform);
        //turn off autoplay at this point:
        if (myBoard.autoPlay)
        {
            myBoard.autoPlay = false;
            myBoard.CancelInvoke("DestroyAuto");
        }

        SortItemList();
        purchasedItems.Clear(); //not sure why this would have anything in it...
        xRotZ = xRotZD;
        yRotZ = yRotZD;
        gTipParts.Stop();
        ChangeLane();
        SetDefaultEventVars();
        GemBuffOption(GameManager.GM.minionDiff); //this will turn 'em all off
        swapXRot = xRot / 2f;
        swapYRot = yRot / 2f;
        refTargetX = dummyHand.transform.localPosition.x;

        //set culling mask for help with backwards numbers:
        LayerMask lm = myCam.cullingMask;
        myCam.cullingMask |= 1 << LayerMask.NameToLayer("DamageNumbersP" + playerNum);
        //myCam.cullingMask = camLayerMask; //this is to help with showing only correct-sided numbers.
        SetShopIcons();
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            print(myBoard.GetRandomItem(6));
        }
        //this isn't ready yet:
        //UpdateBoundAlpha();
        if (EventsManager.EM.countingDown)
        {
            cdTimer.text = StatsManager.SM.FormatTime(EventsManager.EM.eventTimerForText);
        }
        GunEmoteLogic();
        if (refAudio.volume != GameManager.GM.SFXVol())
        {
            refAudio.volume = GameManager.GM.SFXVol();
        }
        //in join menu:
        if (DeviceIsNotNull() && !GameManager.GM.gameIsEnded)
        {
            if (GameManager.GM.onJoinScreen)
            {
                //!this is causing some errors so will have to be looked
                //at eventually.
                PINEntryLogic();
            }

            if (Device.Action2.IsPressed && GameManager.GM.showingIntro)
            {
                cinematicCD -= Time.deltaTime;
                UIManager.UIM.introSkipCircle.fillAmount = (1 - (cinematicCD / cinematicCDOG));
                if (cinematicCD <= 0)
                {
                    GameManager.GM.introOn = false;
                    GameManager.GM.ShowCinematic(null);

                }
            }
            else
            {
                cinematicCD = cinematicCDOG;
                UIManager.UIM.introSkipCircle.fillAmount = 0;

            }

            if (Device.RightStickButton.IsPressed)
            {
                resetTime -= Time.deltaTime;
                if (resetTime <= 0)
                {
                    ClearPlayerOptions();
                    StatsManager.SM.ClearAccountStats(myPIN);
                }
            }
            else
            {
                resetTime = resetOG;
            }

        }
        if ((!DeviceIsNotNull() && Input.GetButton("SkipIntro")) && GameManager.GM.showingIntro)
        {
            cinematicCD -= Time.deltaTime;
            UIManager.UIM.introSkipCircle.fillAmount = (1 - (cinematicCD / cinematicCDOG));
            if (cinematicCD <= 0)
            {
                GameManager.GM.introOn = false;
                GameManager.GM.ShowCinematic(null);

            }
        }
        if (ShopHasPurchaseable() && shopNotif.sprite != shopAvail[0] && !inShop) //checking if in shop cause that overrides the cancel button.
        {
            shopNotif.sprite = shopAvail[0];
            //shopNotif.GetComponent<Animator>().enabled = true;

        }

        //back out of options menu to join menu
        if (DeviceIsNotNull() && Device.Action2.WasPressed || !DeviceIsNotNull() && Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.UIM.inGameOptions)
            {
                //UIManager.UIM.EnterOptions(false, this);
            }
        }


        gunTip.transform.localRotation = new Quaternion(transform.parent.localRotation.x, 0, 0, 0);

        //swap guns:
        if (!DeviceIsNotNull() && Input.GetButtonDown("GunSwap") || DeviceIsNotNull() && Device.Action4.WasPressed)
        {
            if (GameManager.GM.playingGame)
            {
                SwapGun();
            }
        }


        //release zoom button to go back to normal zoom:
        if (Input.GetMouseButtonUp(1) || DeviceIsNotNull() && Device.LeftTrigger.WasReleased
            || (gunAnim.GetBool("HoldingZoom") && Device.RightStickButton.WasPressed))
        {
            StartCoroutine(ZoomCoolDown());
            myCam.fieldOfView = zoomOG;
            gunAnim.SetBool("HoldingZoom", false);
            zoomIMG.SetActive(false);
            dummyHand.gameObject.SetActive(true);

        }

        //if you're not on the rifle, always go back to original zoom.
        if (equippedGun != 1 && myCam.fieldOfView != zoomOG)
        {
            myCam.fieldOfView = zoomOG;
            gunAnim.SetBool("HoldingZoom", false);

            zoomIMG.SetActive(false);
            dummyHand.gameObject.SetActive(true);

        }




        if (!IsHoldingFireButton() && !IsHoldingLeftBumper())
        {

            muzzleFlash.loop = false;
            muzzleFlash.Stop();
            gunAnim.SetBool("HoldShoot", false);
            gTipParts.Stop();
            lineRend.enabled = false;

            if (refAudio.loop && !IsHoldingLeftBumper())
            {
                refAudio.loop = false;
                refAudio.Stop();
            }
        }

        if (!inOptions && !GameManager.GM.gameIsEnded && GameManager.GM.playingGame) //if you're not in the options and the game isn't ended...
        {


            ShootRevealLazer();

            //shooting is different for the guns:
            switch (equippedGun)
            {

                case 0:
                    if (IsHoldingFireButton())
                    {

                        Shoot();
                        gunAnim.SetBool("HoldShoot", true);
                        //audio stuff:
                        if (!refAudio.isPlaying && !refAudio.loop)
                        {
                            refAudio.clip = playerNoises[0];
                            refAudio.loop = true;
                            refAudio.pitch = 1;
                            refAudio.Play();
                        }
                    }

                    break;

                case 1:
                    if (HitFireButton()) //no auto firing with sniper
                    {

                        if (canSnipe)
                        {
                            Shoot();
                            //if(!IsHoldingLeftBumper()) //don't turn it on while zoomed.
                            //{
                            //    dummyHand.SetActive(true);
                            //}
                            gunAnim.SetTrigger("SingleShoot");
                            StartCoroutine(SnipeCD());
                            refAudio.clip = playerNoises[1];
                            refAudio.loop = false;
                            refAudio.pitch = 1;
                            refAudio.Play();
                        }
                    }
                    break;
            }

            //right click to swap gems:
            if (IsHoldingLeftBumper())
            {
                switch (equippedGun)
                {
                    //gem blaster:
                    case 0:
                        if (sJuice > 0 || GameManager.GM.infiniteSwap || infiniteJuice || EventsManager.EM.infiniteSwap) //keeping global and local for flexibility (if one player gets infinite swap boost, ex)
                        {
                            //make swapping rotation slower than normal (if option is on):
                            if (swapAssist)
                            {
                                swapXRot = xRot / 2f;
                                swapYRot = yRot / 2f;
                            }
                            ShootToSwap();
                            gunAnim.SetBool("HoldShoot", true);
                            if (!refAudio.isPlaying && !refAudio.loop)
                            {

                                refAudio.clip = playerNoises[0];
                                refAudio.loop = true;
                                refAudio.pitch = 1.1f;
                                refAudio.Play();
                            }

                        }
                        else
                        {
                            sJuice = 0;
                            firstSetGem = null;
                            secondSetGem = null;
                            isSwapping = false;
                            lineRend.enabled = false;
                            if (gsLR != null)
                            {
                                Destroy(gsLR.gameObject);
                                gsLR = null;
                            }
                        }
                        break;


                    //Sniper rifle:
                    case 1:
                        //move the gun toward the center then zoom:
                        if (!gunAnim.GetBool("HoldingZoom"))
                        {
                            gunAnim.SetBool("HoldingZoom", true);
                        }
                        StartCoroutine(SetZoomDelay(GameManager.GM.GetClipTime(gunAnim, "HoldZoom")));

                        break;
                }

            }

            //click right stick to zoom, too!
            if (equippedGun == 1 && DeviceIsNotNull() && Device.RightStickButton.WasPressed && canZoom)
            {
                if (!gunAnim.GetBool("HoldingZoom"))
                {
                    gunAnim.SetBool("HoldingZoom", true);
                }
                StartCoroutine(SetZoomDelay(GameManager.GM.GetClipTime(gunAnim, "HoldZoom")));

            }


            //this may be nonsense and not used but
            //checking where you let go of the shoot button:
            if (!DeviceIsNotNull() && Input.GetMouseButtonUp(1) || DeviceIsNotNull() && Device.LeftTrigger.WasReleased)
            {
                if (sJuice > 0 || GameManager.GM.infiniteSwap || infiniteJuice || EventsManager.EM.infiniteSwap)
                {
                    UpShoot();
                }
                else
                {
                    firstSetGem = null;
                    secondSetGem = null;
                }
            }

            if (Input.GetMouseButtonDown(2))
            {
                ChangeColor();
            }

            //change lanes:
            if (DeviceIsNotNull() && Device.RightBumper.WasPressed || !DeviceIsNotNull() && Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                laneIndex++;
                ChangeLane();
            }
            if (DeviceIsNotNull() && Device.LeftBumper.WasPressed || !DeviceIsNotNull() && Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                laneIndex--;
                ChangeLane();
            }

            //also, you can use numbers to change lanes on kb:
            LaneChangeKB();
        }

        //lobby options (!this is redundant and i dislike it.  someday we'll refactor):
        if (DeviceIsNotNull() &&
            Device.CommandWasPressed || !DeviceIsNotNull() && Input.GetButtonDown("LobbyOptions"))
        {
            if (GameManager.GM.onJoinScreen)
            {
                if (!UIManager.UIM.inGameOptions)
                {
                    //this is being annoying so i'm just disabling it.
                    //click the options button
                    //UIManager.UIM.gOptionsMenu.GetComponentInChildren<UIScrollToSelectionXY>().events = m_events;

                    UIManager.UIM.InvokeGameOptions(m_events);
                    TurnOffOtherInputs();

                    //UIManager.UIM.EnterOptions(true, this);
                }

            }
        }

        //open options:
        if (DeviceIsNotNull() &&
        Device.CommandWasPressed || !DeviceIsNotNull() && Input.GetButtonDown("Options"))
        {
            //if (GameManager.GM.onJoinScreen)
            //{
            //    if (!UIManager.UIM.inGameOptions)
            //    {
            //        //this is being annoying so i'm just disabling it.
            //        //click the options button
            //        //UIManager.UIM.gOptionsMenu.GetComponentInChildren<UIScrollToSelectionXY>().events = m_events;

            //        UIManager.UIM.InvokeGameOptions(m_events);
            //        TurnOffOtherInputs();

            //        //UIManager.UIM.EnterOptions(true, this);
            //    }

            //}
            //but don't bring the options up if you're zoomed in.
            if (GameManager.GM.playingGame && !gunAnim.GetBool("HoldingZoom"))
            {
                if (optionsPane.activeSelf)
                {
                    ToggleOptions(false);
                    //optionsPane.SetActive(false);
                    //Cursor.lockState = CursorLockMode.Locked;
                    //Cursor.visible = false;
                    //inOptions = false;

                }
                else
                {
                    ToggleOptions(true);

                    //optionsPane.SetActive(true);
                    //inOptions = true;
                    //m_events.SetSelectedGameObject(aimSlider.gameObject);
                    //Cursor.lockState = CursorLockMode.None;
                    //Cursor.visible = true;
                }

            }
        }

        //toggle lane info:
        if (DeviceIsNotNull() && Device.DPadUp.WasPressed || !DeviceIsNotNull() && Input.GetButtonUp("LaneInfo"))
        {
            if (!inOptions && !inShop && GameManager.GM.playingGame)
            {
                myLaneInfo.GetComponent<Animator>().SetBool("LaneIn", !myLaneInfo.GetComponent<Animator>().GetBool("LaneIn"));
            }
        }

        //toggle event info:
        if (DeviceIsNotNull() && Device.DPadRight.WasPressed || !DeviceIsNotNull() && Input.GetButtonUp("EventInfo"))
        {
            if (!inOptions && !inShop && GameManager.GM.playingGame && GameManager.GM.eventsOn)
            {
                myEventCard.GetComponent<Animator>().SetBool("eventIn", !myEventCard.GetComponent<Animator>().GetBool("eventIn"));
            }
        }

        //open item shop:
        if (DeviceIsNotNull() && Device.DPadDown.WasPressed || !DeviceIsNotNull() && Input.GetButtonUp("ItemShop"))
        {
            if (GameManager.GM.playingGame && !inOptions && !inShop && GameManager.GM.itemsOn)
            {
                //if (optionsPane.activeSelf)
                //{
                //    //if you're in the options menu, turn that off first
                //    ToggleOptions(false);
                //}
                //if (itemPane.activeSelf)
                //{
                //    //weird to not close it with the button that opened it but if you want to navigate with dpad...
                //    //ToggleShop(false);
                //}
                //else
                //{
                ToggleShop(true);
                //tuck away the lane info so you can see the items:

                //}

            }
            else if (!DeviceIsNotNull() && Input.GetButtonUp("ItemShop") && inShop)
            {
                ToggleShop(false);
            }

        }


        //extra way to close menus:
        if (DeviceIsNotNull() && Device.Action2.WasPressed)
        {
            if (optionsPane.activeSelf)
            {
                ToggleOptions(false);
            }
            if (itemPane.activeSelf && inShop) //this should be cleaned up
            {
                ToggleShop(false);
                m_events.SetSelectedGameObject(null);
            }
        }


        if (quickRotate && !rotting && (DeviceIsNotNull() && Device.Action3.WasPressed || !DeviceIsNotNull() && Input.GetButtonDown("QuickTurn")))
        {
            Quaternion targetRot = Quaternion.Euler(Vector3.up * 180);
            StartCoroutine(GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().m_MouseLook.QuickRotate(transform.parent, transform, targetRot, spinSpeed));
            StartCoroutine(SpinDelay(spinSpeed));//cooldown so you can't rot with right stick or press it again while mid rotation.

        }


        //right now this is outside of inOptions check.  i'm okay with that.
        if (!rotting && GameManager.GM.playingGame && !kickback)
        {
            RotateWithRightStick(transform.parent, transform);
        }
        //DrawLaser();

        //Quick restart game:
        if (GameManager.GM.gameIsEnded && DeviceIsNotNull())
        {
            if (Device.Action1.IsPressed && Device.Action2.IsPressed && Device.Action3.IsPressed && Device.Action4.IsPressed)
            {
                GameManager.GM.gameIsEnded = false;
                GameManager.GM.ReloadGame();
            }
        }
        if (GameManager.GM.gameIsEnded && !DeviceIsNotNull())
        {
            if (Input.GetButtonDown("Restart"))
            {
                GameManager.GM.gameIsEnded = false;
                GameManager.GM.ReloadGame();
            }
        }

        DisallowClickThrough();
    }

    //was fiddling with this in regards to backing out of join screen but instead i'm just going to reload the game.  much easier.  probably looks worse.
    /*
    private void LateUpdate()
    {
        if (GameManager.GM.onTitleScreen)
        {
            PlayerManager.PM.RemovePlayer(this);
        }
    }
    */
    //public void SliderLogic()
    //{
    //    //this seems so dumb but lets see:
    //    if (m_events.currentSelectedGameObject == aimSlider)
    //    {
    //        if (Device.DPadLeft)
    //        {
    //            aimSlider.value -= .1f;
    //        }
    //    }
    //}

    public void ToggleOptions(bool onOrOff)
    {
        if (!UIManager.UIM.inHowTo)
        {
            if (!inShop)
            {
                optionsPane.SetActive(onOrOff);
                Cursor.visible = onOrOff;
                inOptions = onOrOff;
            }


            if (!onOrOff)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                //bandaid cause if you mess with the options in 2p, kb event system might be off:
                if (!m_events.gameObject.activeSelf)
                {
                    m_events.gameObject.SetActive(true);
                }
                m_events.SetSelectedGameObject(zoomSlider.gameObject);
                Cursor.lockState = CursorLockMode.None;
                if (inShop)
                {
                    ToggleShop(false);
                }
            }
        }
    }
    public void ToggleShop(bool onOrOff)
    {
        //itemPane.SetActive(onOrOff);
        //Cursor.visible = onOrOff;
        if (!UIManager.UIM.inHowTo)
        {
            inShop = onOrOff;
            if (!itemPane.activeSelf && onOrOff)
            {
                itemPane.SetActive(true);
            }
            itemPane.GetComponent<Animator>().SetBool("ShopIn", onOrOff);
            if (!onOrOff)
            {
                Cursor.lockState = CursorLockMode.Locked;
                myLaneInfo.GetComponent<Animator>().SetBool("LaneIn", true);
                if (!ShopHasPurchaseable())
                {
                    shopNotif.sprite = shopReg[2];
                    if (DeviceIsNotNull())
                    {
                        shopControl.sprite = shopReg[1];

                    }
                    else
                    {
                        shopControl.sprite = shopReg[0]; //kbam == f key
                    }
                    //shopNotif.GetComponent<Animator>().enabled = false;
                }
            }
            else
            {
                //assign the event system that's going to be navigating the shop"

                itemPane.GetComponentInChildren<UIScrollToSelectionXY>().events = m_events;
                //bandaid cause if you mess with the options in 2p, kb event system might be off:
                if (!m_events.gameObject.activeSelf)
                {
                    m_events.gameObject.SetActive(true);
                }
                m_events.SetSelectedGameObject(firstItem.gameObject);
                Cursor.lockState = CursorLockMode.None;
                myLaneInfo.GetComponent<Animator>().SetBool("LaneIn", false);
                if (DeviceIsNotNull())
                {
                    shopControl.sprite = shopCancel[1];
                }
                //else
                //{
                //    shopNotif.sprite = shopCancel[0];

                //}
                //check if any should unlock:
                ItemLockCheck();
            }

        }
    }
    //putting this in a separate function so
    //when you purchase an item, it can call this
    //to check if other items should lock.
    public void ItemLockCheck()
    {
        foreach (ItemHolder ih in itemPane.GetComponentsInChildren<ItemHolder>())
        {
            ih.LockMe();
        }
    }

    public void SwapGun()
    {
        if (GameManager.GM.playingGame)
        {
            //update Anim, as well:
            gunAnim.SetTrigger("GunOut");

        }

    }

    //this is called at end of GunOut animation
    public void TurnOnGun()
    {
        //go to the next gun in the list or else cycle back to 0:
        if (equippedGun < guns.Length - 1)
        {
            equippedGun++;
        }
        else
        {
            equippedGun = 0;
        }
        foreach (GameObject g in guns)
        {
            g.SetActive(false);
        }
        //gotta wait a sec for this.  programming the wait in the animation for now (blank space keyframes).

        guns[equippedGun].SetActive(true);

        gunAnim.SetTrigger("GunIn");
    }

    public bool DeviceIsNotNull()
    {
        if (Device != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    void LaneChangeKB()
    {
        if (!DeviceIsNotNull() && Input.GetKeyDown(KeyCode.Alpha1))
        {
            laneIndex = 0;
            ChangeLane();
        }
        if (!DeviceIsNotNull() && Input.GetKeyDown(KeyCode.Alpha2))
        {
            laneIndex = 1;
            ChangeLane();
        }
        if (!DeviceIsNotNull() && Input.GetKeyDown(KeyCode.Alpha3))
        {
            laneIndex = 2;
            ChangeLane();
        }
        if (!DeviceIsNotNull() && Input.GetKeyDown(KeyCode.Alpha4))
        {
            laneIndex = 3;
            ChangeLane();
        }
    }
    void ChangeLane()
    {
        if (laneIndex > tmbj.Length - 1)
        {
            laneIndex = 0;
        }
        else if (laneIndex < 0)
        {
            laneIndex = tmbj.Length - 1;
        }
        //print(laneIndex);
        for (int i = 0; i < tmbj.Length; i++)
        {
            tmbj[i].enabled = false;
        }
        tmbj[laneIndex].enabled = true;
        myBoard.TurnOnLaneHighlight(laneIndex);
    }

    //this is to turn on the zoom after the animation
    public IEnumerator SetZoomDelay(float del)
    {
        yield return new WaitForSeconds(del);
        //don't do this if the player stopped zooming early:
        if (gunAnim.GetBool("HoldingZoom"))
        {
            dummyHand.gameObject.SetActive(false);

            myCam.fieldOfView = zoomAmt;
            if (!zoomIMG.activeSelf)
            {
                zoomIMG.SetActive(true);
                //i want to retain the perspective of the scope but fill the screen if 2+ players:
                if (PlayerManager.PM.players.Count > 1)
                {
                    zoomIMG.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                }
                else
                {
                    zoomIMG.transform.localScale = Vector3.one;

                }
            }

        }
    }

    public void Init(Transform character, Transform camera)
    {
        charTarget = character.localRotation;
        camTargetRot = camera.localRotation;
    }
    //tried to copy this from MouseLook.cs and modify it for controllers:
    void RotateWithRightStick(Transform character, Transform cam)
    {
        if (DeviceIsNotNull())
        {
            float yMod;
            float xMod;
            //if you're zooming:
            if (equippedGun == 1 && gunAnim.GetBool("HoldingZoom")/*IsHoldingLeftBumper()*/)
            {
                yMod = Device.RightStickX * xRotZ;
                xMod = Device.RightStickY * yRotZ;
            }
            else
            {
                if (isSwapping && swapAssist) {
                    yMod = Device.RightStickX * swapXRot;
                    xMod = Device.RightStickY * swapYRot;

                }
                else
                {
                    yMod = Device.RightStickX * xRot;
                    xMod = Device.RightStickY * yRot;

                }
            }

            if (isInverted)
            {
                xMod *= -1;
            }

            charTarget *= Quaternion.Euler(0f, yMod, 0f);
            camTargetRot *= Quaternion.Euler(-xMod, 0f, 0f);

            camTargetRot = ClampRotationAroundXAxis(camTargetRot);


            character.localRotation = charTarget;
            cam.localRotation = camTargetRot;
            //print(xMod + " X");
            //print(yMod + " Y");
        }
    }

    Quaternion ClampRotationAroundXAxis(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

        angleX = Mathf.Clamp(angleX, minX, maxX);

        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }

    public void UpdateAimSensitivity()
    {
        xRot = xRotD * aimSlider.value; //aim slider is from .1-2
        yRot = yRotD * aimSlider.value;
        //for now, laziness is imploring me to just half the zoom sensitivity instead of separately controlling it.
        //!in the future, separate sliders for zoomRot will win me accessibility awards:
        xRotZ = xRotZD * zoomSlider.value;
        yRotZ = yRotZD * zoomSlider.value;


        mouseControl.XSensitivity = xMRotD * xRot;
        mouseControl.YSensitivity = yMRotD * yRot;

        SaveOptions(myPIN);//just to fill that pp entry with something for when we check with other GetPP functions.
        PlayerPrefs.SetFloat(myPIN + "zoom", zoomSlider.value);
        PlayerPrefs.SetFloat(myPIN + "aim", aimSlider.value);

    }

    void ShootRevealLazer()
    {
        RaycastHit hit;
        Ray toCam = myCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        //!!!This is colliding with the death floor and so you can't shoot
        //some pipes.  messed around with layermask for the ray but couldn't get it
        //and getting frustrated.
        Physics.Raycast(toCam, out hit);
        Color temp = new Color();

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Gem"))
            {
                Gem hitting = hit.collider.GetComponent<Gem>();

                temp = hitting.applyColorHere.materials[0].color;
                hitting.applyColorHere.materials[0].color = Color.white;
                if (prevGem == null)
                {
                    if (currGem != null)
                    {
                        prevGem = currGem;

                    }
                    //prevGem.RevertMyColors();
                }
                else
                {
                    if (!prevGem.blinded)
                        prevGem.RevertMyColors();
                    prevGem = currGem;
                    prevGem.applyColorHere.materials[0].color = temp;

                }

                if (hitting.blinded)
                {
                    if (prevGem == null)
                    {
                        prevGem = hitting;
                        prevGem.RiddleMe(false); //turn off blind canvas
                    }
                    else
                    {
                        prevGem = currGem;
                        if (prevGem.blinded)
                            prevGem.RiddleMe(true);
                    }
                    currGem = hitting;
                    currGem.RiddleMe(false);

                }
                else
                {
                    currGem = hitting;

                }

            }
            else
            {
                //revert prev gem color (after swapping, ex.
                if (prevGem != null && !prevGem.blinded)
                {
                    prevGem.RevertMyColors();

                }
                if (currGem != null && !currGem.blinded)
                {
                    currGem.RevertMyColors();
                }
            }

            if (whiteRetTags.Contains(hit.collider.tag))
            {
                reticle.color = Color.white;
            }
            else
            {
                reticle.color = Color.black;
            }



            //if(hit.collider.CompareTag("Minion") || hit.collider.CompareTag("Target") 
            //|| hit.collider.CompareTag("WeakSpot") || hit.collider.CompareTag("Ship") 
            //|| hit.collider.CompareTag("Target")
            //    || hit.collider.CompareTag("PeonHouse") || hit.collider.CompareTag("PeonUpgrade")
            //|| hit.collider.CompareTag("Lode")
            //   || hit.collider.CompareTag("BubbleShield") || hit.collider.CompareTag("Pipe"))
            //{
            //    reticle.color = Color.white;
            //}else{
            //    reticle.color = Color.black;
            //}
        }
        else
        {
            reticle.color = Color.black;
        }
    }

    void Shoot()
    {
        //!!I cannot for the life of me figure out what is turning on/off the muzzle flash/
        RaycastHit hit;
        //Physics.Raycast(gunTip.transform.position, transform.TransformDirection(Vector3.forward), out hit);
        Ray toCam = myCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Physics.Raycast(toCam, out hit);
        //Debug.DrawRay(gunTip.transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.blue);



        //muzzleFlash.loop = true;
        if (equippedGun == 1)
        {
            muzzleFlash.Play();//play the muzzle flash with the sniper even if you don't hit anything.
            if (gunAnim.GetBool("HoldingZoom"))
            {
                sniperJuiceParts.Play();
            }
            if (rumbleOn)
            {
                StartCoroutine(PlayerManager.PM.Vibrate(Device, .2f, .2f));
            }
        }

        if (hit.collider != null)
        {
            //if it's the lasergun, shoot a laser!
            if (equippedGun == 0)
            {
                DrawLaser(shootMat, hit.point, true);
            }


            if (hit.collider.CompareTag("PeonHouse"))
            {
                hit.collider.GetComponent<PeonSpawner>().CostDelay(0);
            }
            if (hit.collider.CompareTag("PeonUpgrade"))
            {
                hit.collider.GetComponentInParent<PeonSpawner>().CostDelay(1);
            }
            if (hit.collider.CompareTag("SwapPotPurchase"))
            {
                hit.collider.GetComponent<PurchaseGeneric>().CostDelay(this);
            }
            if (hit.collider.CompareTag("Lode"))
            {
                //not a fan of this right here but we should spawn the bolt to have the enemy team's number:
                //if (teamNum == 0)
                //{
                hit.collider.GetComponent<SimpleInstantiate>().TakeDamage(damage, "Bolt" + teamNum);

                //}
                //else
                //{
                //    hit.collider.GetComponent<SimpleInstantiate>().TakeDamage(damage, "Bolt" + 0);

                //}
            }

            //turn on vacuum
            if (hit.collider.CompareTag("Vac"))
            {
                hit.collider.GetComponent<Attractor>().Attract();
            }

            //refill swapjuice if shooting potion
            if (hit.collider.CompareTag("SwapPot"))
            {
                RefillSwapJuice(juiceFill);
                hit.collider.GetComponentInChildren<Animator>().SetTrigger("Squeeze");
                hit.collider.GetComponentInChildren<ParticleSystem>().Emit(50);
            }

            //Gems take damage:
            if (hit.collider.CompareTag("Gem"))
            {
                // hit.collider.GetComponent<GemBehaviour>().TakeDamage(damage * multiplier);
                hit.collider.GetComponent<Gem>().TakeDamage(damage + GameManager.GM.teamBuffs[teamNum].p_damageBuff, this);
            }

            if (hit.collider.CompareTag("Ice"))
            {
                if (equippedGun == 0)
                {
                    hit.collider.GetComponent<ShootToDisable>().TakeDamage(damage, this);

                }
                else
                {
                    hit.collider.GetComponent<ShootToDisable>().TakeDamage(SniperDamage(), this);

                }
            }
            if (hit.collider.CompareTag("GasTank"))
            {
                if (equippedGun == 0)
                {
                    hit.collider.GetComponent<ShootToDisable>().TakeDamage(damage, this);

                }
                else
                {
                    hit.collider.GetComponent<ShootToDisable>().TakeDamage(SniperDamage(), this);

                }
            }

            if (hit.collider.CompareTag("Peon"))
            {
                if (equippedGun == 1)
                {
                    hit.collider.GetComponent<PeonBehaviour>().Die();
                }
            }

            if (hit.collider.CompareTag("Target"))
            {
                if (equippedGun == 1)
                {
                    hit.collider.GetComponent<ShootToDisable>().TakeDamage(SniperDamage(), this);
                    EventsManager.EM.targetsBroken[teamNum]++;
                    EventsManager.EM.activatedTargets.Remove(hit.collider.gameObject);
                }
            }
            if (hit.collider.CompareTag("Hat"))
            {
                if (equippedGun == 1)
                {
                    float x = Random.Range(-200, 200);
                    float z = Random.Range(-200, 200);
                    hit.transform.SetParent(null);
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    rb.AddForceAtPosition(new Vector3(x, 600, z), hit.transform.position);
                    rb.AddTorque(rb.transform.right * 400);
                    rb.useGravity = true;
                    hit.collider.isTrigger = false;
                    Destroy(hit.transform.gameObject, 3f);
                }
            }
            if (hit.collider.CompareTag("BubbleShield"))
            {
                if (equippedGun == 1)
                {
                    hit.transform.GetComponentInParent<Enemy>().BurstBubble();
                }
            }

            //damage and slowing minions:
            if (hit.collider.CompareTag("Minion"))
            {
                if (rumbleOn)
                {
                    StartCoroutine(PlayerManager.PM.Vibrate(Device, .4f, .2f));
                }
                Enemy e = hit.collider.GetComponentInParent<Enemy>();
                if (equippedGun == 1)
                {
                    if (e.playerTeam != teamNum)
                    {
                        if (IsHoldingLeftBumper())
                        {
                            e.TakeDamage(SniperDamage() * 2, this); //deal double damage if you're zoomed in.
                        }
                        else
                        {
                            e.TakeDamage(SniperDamage(), this); //deal reg damage if you're not.


                        }
                        if (slowRounds && !e.isSlowed)
                        {
                            StartCoroutine(e.GetSlowed());
                        }
                        if (dotRounds && !e.isPoisoned)
                        {
                            e.StartCoroutine(e.GetPoisoned()); //i think starting it from within the enemy is right here, cause the enemy will cancel the coroutine.
                        }

                        //instantate some electric particles on hit.
                        Instantiate(enemyHitParts, hit.point, Quaternion.identity);
                    }
                    else
                    {
                        if (e.waitingToBeSacd)
                        {
                            e.DeathStuff();
                        }
                        if (healingRounds && !e.IsAtMaxHealth())
                        {
                            e.TakeDamage(-SniperDamage(), this);
                        }
                    }
                    GameManager.GM.PlayClip(14, .4f);

                }
                if (equippedGun == 0 && EventsManager.EM.lazarDamages && e.playerTeam != teamNum)
                {
                    e.TakeDamage(damage, this);
                }


            }

            //extra dmg for weak spot:
            if (hit.collider.CompareTag("WeakSpot"))
            {
                Enemy e = hit.collider.GetComponentInParent<Enemy>();
                GameManager.GM.PlayClip(14, .4f);
                if (rumbleOn)
                {
                    StartCoroutine(PlayerManager.PM.Vibrate(Device, .4f, .2f));
                }


                if (equippedGun == 1 && e.playerTeam != teamNum)
                {
                    if (IsHoldingLeftBumper())
                    {
                        e.TakeDamage(SniperDamage() * 4, this); //if zoomed in, take more dmg
                    }
                    else
                    {
                        e.TakeDamage(SniperDamage() * 3, this);

                    }
                    if (slowRounds && !e.isSlowed)
                    {
                        StartCoroutine(e.GetSlowed());
                    }
                    if (dotRounds && !e.isPoisoned)
                    {
                        e.StartCoroutine(e.GetPoisoned()); //i think starting it from within the enemy is right here, cause the enemy will cancel the coroutine.
                    }

                }
                if (equippedGun == 1 && e.playerTeam == teamNum)
                {
                    if (e.waitingToBeSacd)
                    {
                        e.DeathStuff();
                    }
                    if (healingRounds && !e.IsAtMaxHealth())
                    {
                        e.TakeDamage(-SniperDamage(), this);
                    }
                }
            }

            if (hit.collider.CompareTag("Ship") && equippedGun == 1)
            {
                hit.collider.GetComponent<ShipCrash>().crashIntoMe = true;
                //Destroy(hit.collider.gameObject, 4f);
            }
            //cool environ stuff with shootin the tube system:
            if (hit.collider.CompareTag("Pipe") && equippedGun == 1)
            {
                hit.collider.GetComponent<GlassSmash>().TakeDamage();
                hit.collider.GetComponent<MeshRenderer>().material.color = Color.red;
                //Destroy(hit.collider.gameObject);
            }

            //heal the towers with healing rounds:
            if (hit.collider.CompareTag("TowerTop"))
            {
                if (healingRounds && equippedGun == 1)
                {
                    Tower t = hit.collider.GetComponentInParent<Tower>();
                    if (t.health < t.mxHealth)
                    {
                        t.TakeDamage(-sniperD); //i love taking -damage = healing.
                    }
                }
            }

            //if you sniped, make a trail daddy:
            if (equippedGun == 1)
            {

                GameObject p = Instantiate(gunTrailDad, hit.point, Quaternion.identity);
                //print("shot hit " + hit.transform.name);
                //also make a childed trail and parent it to the hitpoint.
                if (IsHoldingLeftBumper())
                {
                    //if zoomed, shoot it from the center of the screen (a bit down) instead of from the side
                    Instantiate(gunTrailChild, transform.parent.position + centerTrailOffset, Quaternion.identity, p.transform);
                }
                else
                {
                    Instantiate(gunTrailChild, gunTip.transform.position, Quaternion.identity, p.transform);
                }
                Destroy(p, 2f);
                ApplyRecoil();

            }
        }
        else
        {
            if (lineRend.enabled)
            {
                lineRend.enabled = false;
            }
        }

    }

    public float SniperDamage()
    {
        return sniperD + GameManager.GM.teamBuffs[teamNum].p_snipeDMG;
    }

    //some helper device functions:
    public bool IsHoldingLeftBumper()
    {
        if (gunAnim.GetBool("HoldingZoom") || !DeviceIsNotNull() && Input.GetMouseButton(1) || DeviceIsNotNull() && Device.LeftTrigger) //if you're holding zoom button down...
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool HitFireButton()
    {
        if (!DeviceIsNotNull() && Input.GetMouseButtonDown(0) || DeviceIsNotNull() && Device.RightTrigger.WasPressed)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsHoldingFireButton()
    {
        if (!DeviceIsNotNull() && Input.GetMouseButton(0) || DeviceIsNotNull() && Device.RightTrigger)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void ShootToSwap()
    {
        //so it doesn't go negative during infinite swap times:
        if (sJuice > 0)
        {
            sJuice -= Time.deltaTime * juiceMod;
        }
        sJuiceFill.fillAmount = (sJuiceOG - (sJuiceOG - sJuice)) / 100;

        RaycastHit hit;
        //Physics.Raycast(gunTip.transform.position, transform.TransformDirection(Vector3.forward), out hit);
        Ray toCam = myCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Physics.Raycast(toCam, out hit);

        if (hit.collider != null)
        {
            DrawLaser(swapMat, hit.point, false);

            if (hit.collider.CompareTag("Lode"))
            {
                hit.collider.GetComponent<SimpleInstantiate>().TakeDamage(damage * 2, "Bolt" + teamNum);


            }

            if (hit.collider.CompareTag("PeonHouse"))
            {
                hit.collider.GetComponent<PeonSpawner>().CostDelay(2);
            }
            if (hit.collider.CompareTag("PeonUpgrade"))
            {
                hit.collider.GetComponentInParent<PeonSpawner>().CostDelay(3);
            }
            if (hit.collider.CompareTag("Gem"))
            {

                //hit.collider.GetComponent<Gem>().SetFirstTouch(hit.collider.transform.position);
                if (gsLR == null && !isSwapping)
                {
                    firstSetGem = hit.collider.GetComponent<Gem>();
                    firstSetGem.brd.myPlayer = this;
                    //line renderer to show you're swapping:
                    GameObject l = Instantiate(gemSwapLR, firstSetGem.brd.transform, false);
                    for (int i = 0; i < l.GetComponent<LineRenderer>().positionCount; i++)
                    {
                        l.GetComponent<LineRenderer>().SetPosition(i, Vector3.zero); //set the initial position to 0 so it doesn't flash into existence.

                    }
                    l.transform.localPosition = Vector3.zero;
                    gsLR = l.GetComponent<LRFollowObj>();
                    isSwapping = true;
                }
                else
                {
                    //print("assigning: " + hit.collider.transform.position);
                    AssignLRPosition(hit.collider.transform.localPosition);
                }

            }
            else if (hit.collider.CompareTag("TowerTop"))
            {
                //print("supercharging");
                hit.collider.GetComponentInParent<Tower>().ChargeUp();
                //Debug.DrawLine(gunTip.transform.position, hit.collider.transform.position, Color.cyan);
            }
            else
            {
                //firstSetGem = null;
                //isSwapping = false;
            }
        }else{
            if(lineRend.enabled)
            {
                lineRend.enabled = false;
            }
        }
    }
    void UpShoot()
    {
        RaycastHit hit;
        //Physics.Raycast(gunTip.transform.position, transform.TransformDirection(Vector3.forward), out hit);
        //Debug.DrawRay(gunTip.transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.blue);

        Ray toCam = myCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Physics.Raycast(toCam, out hit);


        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Gem"))
            {
                //hit.collider.GetComponent<GemBehaviour>().TakeDamage(damage * multiplier);
                //hit.collider.GetComponent<Gem>().SetLastTouch(hit.collider.transform.position);
                if (hit.collider.GetComponent<Gem>() != firstSetGem)
                {
                    secondSetGem = hit.collider.GetComponent<Gem>();
                }
                if ((firstSetGem != null && secondSetGem != null) && firstSetGem.brd.currentState == GameState.move)
                {
                    secondSetGem.brd.SwapGems(firstSetGem, secondSetGem);
                    //StatsManager.SM.pStats[playerNum, 7]++;

                    //!! atm AI does not swap or log this stat.
                    StatsManager.SM.IncrementStat(playerNum, 7);

                }
            }
            else
            {
                firstSetGem = null;
                secondSetGem = null;
            }

        }
        if (gsLR)
        {
            Destroy(gsLR.gameObject);
            gsLR = null;

        }
        isSwapping = false; //this needs to be false no matter what, on button up

    }

    // Changes color of gem
    void ChangeColor()
    {
        RaycastHit hit;
        //Physics.Raycast(gunTip.transform.position, transform.TransformDirection(Vector3.forward), out hit);
        //Debug.DrawRay(gunTip.transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.blue);
        Ray toCam = myCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Physics.Raycast(toCam, out hit);


        if (hit.collider != null && hit.collider.CompareTag("Gem"))
            hit.collider.GetComponent<GemBehaviour>().ChangeColor();
    }

    void DrawLaser(Material c, Vector3 endPoint, bool showParticles)
    {

        lineRend.enabled = true;
        lineRend.material = c;
        Vector3 to = endPoint;
        lineRend.SetPosition(0, gunTip.transform.position);
        lineRend.SetPosition(1, to);

        //this is also firing the muzzle flash for a frame...
        if (!gTipParts.isPlaying && showParticles)
            gTipParts.Play();

    }

    public void AssignLRPosition(Vector3 pos)
    {
        gsLR.GetComponent<LineRenderer>().SetPosition(0, firstSetGem.transform.localPosition);
        gsLR.posToFollow = pos;
    }


    public void RefillSwapJuice(float percentage)
    {
        float p = sJuiceOG - (sJuiceOG - percentage);
        if (sJuice + p < sJuiceOG)
        {
            sJuice += p;

        }
        else
        {
            sJuice = sJuiceOG;
        }
        sJuiceFill.fillAmount = sJuice / 100;
    }

    public void ApplyRecoil()
    {
        if (!DeviceIsNotNull())
        {
           mouseControl.m_CameraTargetRot *= Quaternion.Euler(-(kickbackAmount + GameManager.GM.teamBuffs[teamNum].p_kickBack), 0f, 0f);
            
        }
        camTargetRot *= Quaternion.Euler(-(kickbackAmount+ GameManager.GM.teamBuffs[teamNum].p_kickBack), 0f, 0f);
        myCam.transform.localRotation = camTargetRot;
        StartCoroutine(KickbackCD());
       // myCam.localRotation = myCam.transform.rotation * Quaternion.Euler(Vector3.right * -kickbackAmount);
    }
    IEnumerator KickbackCD()
    {
        kickback = true;
        mouseControl.kickback = true;
        yield return new WaitForSeconds(kickbackTime);
        mouseControl.kickback = false;
        kickback = false;
    }

    IEnumerator SnipeCD()
    {
        canSnipe = false;
        yield return new WaitForSeconds(snipeCD);
        canSnipe = true;
    }
    public void SwapInvert()
    {
        isInverted = !isInverted;
        PlayerPrefsX.SetBool(myPIN + "invert", isInverted);
        SaveOptions(myPIN);
    }
    //these should all really take a toggle:
    public void SwapSwapAssist(Toggle t)
    {
        swapAssist = t.isOn;
        PlayerPrefsX.SetBool(myPIN + " swapAssist", swapAssist);
        SaveOptions(myPIN);
    }
    public void SwapQRot()
    {
        quickRotate = !quickRotate;
    }
    public void SwapRumble()
    {
        rumbleOn = !rumbleOn;
    }

    public void FireNotification(int c, int howMany)
    {
        //this is going to need to spawn a new prefab of text (in case many fire one after the other)
        GameObject n = Instantiate(notifPrefab, myWorldCan.transform);
        Destroy(n, 2f); //kill it after a couple of seconds.
        string numOfPluses = string.Empty;

        //if there's another one at the exact same time, move this one down a bit:
        foreach (Transform t in myWorldCan.GetComponentsInChildren<Transform>())
        {
            if (t.name == n.name && t != n.transform)
            {
                n.GetComponent<RectTransform>().pivot = new Vector3(.5f, n.GetComponent<RectTransform>().pivot.y + 1.5f);
            }
        }
        for (int i = 3; i < howMany; i++)
        {
            numOfPluses += "+";
        }
        Text nt = n.GetComponent<Text>();
        nt.text = GameManager.GM.possibleColors[c].name + numOfPluses + " minion spawned";
        if (GameManager.GM.possibleColors[c].name == "Black") //black on black is too hard to read.
        {
            nt.color = Color.grey;
        }
        else
        {
            nt.color = GameManager.GM.actualColors[c];
        }

        nt.GetComponentInChildren<Image>().color = GameManager.GM.actualColors[c];

    }

    public void FireNotification(string customText, Color customColor)
    {
        //this is going to need to spawn a new prefab of text (in case many fire one after the other)
        GameObject n = Instantiate(notifPrefab, myWorldCan.transform);
        Destroy(n, 2f); //kill it after a couple of seconds.

        //if there's another one at the exact same time, move this one down a bit:
        foreach (Transform t in myWorldCan.GetComponentsInChildren<Transform>())
        {
            if (t.name == n.name && t != n.transform)
            {
                n.GetComponent<RectTransform>().pivot = new Vector3(.5f, n.GetComponent<RectTransform>().pivot.y + 1.5f);
            }
        }

        Text nt = n.GetComponent<Text>();
        nt.text = customText;
        if (customColor != null)
            nt.color = customColor;
        nt.GetComponentInChildren<Image>().color = customColor;

    }

    public void FlashUnderAttack(string whatLane)
    {
        if (attackWarning)
        {
            warningIMG.SetTrigger("FlashGo");
            if (string.IsNullOrEmpty(whatLane))
            {
                FireNotification(whatLane + " Our CORE is under attack!", Color.white);

            }
            else
            {
                FireNotification(whatLane + " lane is under attack", Color.white);
            }
            StartCoroutine(UnderAttackDelay());
        }
    }

    public IEnumerator UnderAttackDelay()
    {
        attackWarning = false;
        yield return new WaitForSeconds(2f);
        attackWarning = true;
    }

    public IEnumerator SpinDelay(float delay)
    {
        rotting = true;
        yield return new WaitForSeconds(delay);
        charTarget = transform.parent.localRotation;
        camTargetRot = transform.localRotation;
        rotting = false;
    }

    void PINEntryLogic()
    {
        if (m_events.currentSelectedGameObject.GetComponent<PinEnterHelper>()!=null)
        {
            if (Device.Action1.WasPressed)
            {
                pinButton.GetComponent<PinEnterHelper>().IncremenetP(0);
            }
            if (Device.Action2.WasPressed)
            {
                pinButton.GetComponent<PinEnterHelper>().IncremenetP(1);
            }
            if (Device.Action3.WasPressed)
            {
                pinButton.GetComponent<PinEnterHelper>().IncremenetP(2);
            }
            if (Device.Action4.WasPressed)
            {
                pinButton.GetComponent<PinEnterHelper>().IncremenetP(3);
            }
        }
    }

    void SaveOptions(string pin)
    {
        PlayerPrefs.SetString(pin, "1");
    }

    public void LoadOptions(string pin)
    {
        if (PlayerPrefs.HasKey(pin))
        {
            zoomSlider.value = PlayerPrefs.GetFloat(pin + "zoom");
            aimSlider.value = PlayerPrefs.GetFloat(pin + "aim");
            isInverted = PlayerPrefsX.GetBool(pin + "invert");
            //swapAssist = PlayerPrefsX.GetBool(pin + "swapAssist");

            UpdateAimSensitivity();
        }
        else
        {
            print("No options found for pin " + myPIN);
        }
    }

    void ClearPlayerOptions()
    {
        if (PlayerPrefs.HasKey(myPIN))
        {
            PlayerPrefs.DeleteKey(myPIN);
            PlayerPrefs.DeleteKey(myPIN + "zoom");
            PlayerPrefs.DeleteKey(myPIN + "aim");
            PlayerPrefsX.SetBool(myPIN + "invert", false);
            StartCoroutine(DebugMessage(myPIN + " prefs reset."));
            print(myPIN + " prefs reset.");
        }
    }

    public IEnumerator DebugMessage(string message)
    {
        GameObject.Find("DebugText").GetComponent<Text>().enabled = true;
        GameObject.Find("DebugText").GetComponent<Text>().text = message;
        yield return new WaitForSeconds(2f);
        GameObject.Find("DebugText").GetComponent<Text>().enabled = false;


    }

    //loop through all the players in the game
    //if they are on a different team, add 'em to your list.
    //Board.cs is going to use otherPlayers list to update LaneInfoHelper
    public void FindOpposingPlayers()
    {
        foreach (PlayerShoot ps in PlayerManager.PM.players)
        {
            if (ps.teamNum != teamNum)
            {
                otherPlayers.Add(ps);
            }
        }
    }

    public void TurnOffOtherInputs()
    {
        foreach (EventSystem ic in FindObjectsOfType<EventSystem>())
        {
            //turn off other event systems while one player is in the options.
            if (ic != UIManager.UIM.gOptionsMenu.GetComponentInChildren<UIScrollToSelectionXY>().events)
            {
                UIManager.UIM.tempOffMods.Add(ic.gameObject);
                ic.gameObject.SetActive(false);
            }
        }
    }

    //so we have something to go back to when events are no longer active:
    public void SetDefaultEventVars()
    {
        def_icon = icon.sprite;
        def_main = main.sprite;
        def_helper = helper.sprite;
        def_name = cName.text;
        def_effect = effect.text;
    }

    public void AssignMiniCard(Sprite i, Sprite m, Sprite h, string cn, string eff)
    {
        icon.sprite = i;
        main.sprite = m;
        helper.sprite = h;
        cName.text = cn;
        effect.text = eff;
    }

    public void ResetEventInfo()
    {
        icon.sprite = def_icon;
        main.sprite = def_main;
        helper.sprite = def_helper;
        cName.text = def_name;
        effect.text = def_effect;
        cdTimer.text = "WAIT";
    }

    //This is probably also when we should increase same tier bolt prices.
    public void SortItemList()
    {
        possibleItems.Clear();
        foreach (ItemHolder ih in GetComponentsInChildren<ItemHolder>(true))
        {
            if(!ih.previouslyPurchased)
            {
                possibleItems.Add(ih.myItem);
            }else{
                purchasedItems.Add(ih.myItem);
            }
        }
        //sort 'em by boltPrice so we can get the cheapest easily:
        possibleItems.Sort((x, y) => x.boltPrice.CompareTo(y.boltPrice));
        //for(int i=0; i < possibleItems.Count; i++)
        //{
        //    if (possibleItems[i].itemTier <= myBoard.ResearchLevel() && GameManager.GM.teamBolts[teamNum]>=possibleItems[i].boltPrice)
        //    {
        //        cheapestItem = possibleItems[i];
        //        print("cheapest = " + possibleItems[i].name);
        //        return;
        //    }
        //}
        cheapestItem = possibleItems[0];
        if (!ShopHasPurchaseable() && inShop)
        {
            shopNotif.sprite = shopReg[2];
            if (DeviceIsNotNull())
            {
                shopControl.sprite = shopCancel[1];
            }
            else
            {
                shopControl.sprite = shopCancel[0];

            }
        }

    }

    public void UpdateItemPrices(int tierJustPurchased)
    {
        foreach (ItemHolder ih in GetComponentsInChildren<ItemHolder>(true))
        {
            if (ih.myItem.itemTier == tierJustPurchased)
            {
                //50% increase to other tier items
                int increase = ih.myItem.ogBoltPrice / 2;
                ih.myItem.boltPrice += increase;
                ih.ShowPriceIncrease(increase);
            }
        }
    }

    public void SetShopIcons()
    {
        if (DeviceIsNotNull())
        {
            shopControl.sprite = shopReg[1];
        }
        else
        {
            shopControl.sprite = shopReg[0];
        }
    }

    public bool ShopHasPurchaseable()
    {
        //there should be a research check here
        // && myBoard.ResearchLevel()>=cheapestItem.itemTier
        //but i'm not getting it to work properly atm.  need a break.
        if (GameManager.GM.teamBolts[teamNum] >= cheapestItem.boltPrice)
        {
            if (!inShop)
            {
                shopNotif.GetComponent<Animator>().enabled = true;
            }
            else
            {
                shopNotif.GetComponent<Animator>().enabled = false;

            }
            //print("shop has purchasable, which is " + cheapestItem.name);
            return true;
        }
        else
        {
            shopNotif.GetComponent<Animator>().enabled = false;

            return false;
        }
    }

    void GunEmoteLogic()
    {
        if (myLaneInfo.totalMinions > 2)
        {
            SetGunEmote(1);
        }else if (waitingForSac)
        {
            SetGunEmote(2);
        }else if (GameManager.GM.everythingPause)
        {
            SetGunEmote(3);
        }else if (GameManager.GM.boltsOnField[teamNum] > 9)
        {
            SetGunEmote(4);
        }
        else
        {
            SetGunEmote(0);
        }
    }

    /// <summary>
    /// 0=happy; 1=enemy alert; 2=research; 3=question mark; 4=money alert
    /// </summary>
    /// <param name="which"></param>
    public void SetGunEmote(int which)
    {
        if(gunEmote.sprite != emoteSprites[which])
        gunEmote.sprite = emoteSprites[which];
    }

    public void SwapHands(Toggle t)
    {
        refTargetX *= -1;
        if(refCR!=null)StopCoroutine(refCR);
        //StopAllCoroutines();
        isLefty = t.isOn;
        refCR = StartCoroutine(SwapHandsTo(spinSpeed, refTargetX));
    }

    public IEnumerator SwapHandsTo(float time, float targetX)
    {
        //!!!Clean this up, probably.  not smart-looking:
        float elapsed = 0f;
        //this math took me like 20 minutes of drawing on paper:
        //do your homework, kids!
        while (Mathf.Abs(dummyHand.localPosition.x- targetX)>Mathf.Epsilon)
            {
            dummyHand.localPosition = Vector3.Lerp(dummyHand.localPosition, new Vector3(targetX, dummyHand.localPosition.y, dummyHand.localPosition.z), elapsed/time);
                elapsed += Time.deltaTime;
                yield return null;
            }

    }

    public void GemBuffOption(Toggle t)
    {
        myBoard.GemBuffImageSwap(t.isOn);
        gemBuffHints = t.isOn;
    }
    public void GemBuffOption(bool t)
    {
        myBoard.GemBuffImageSwap(t);
        gemBuffHints = t;
    }

    public void EnableQuit()
    {
        UIManager.UIM.QuitNotif(this);
    }
    public void EnableHowTo()
    {
        UIManager.UIM.ShowHowToOnScreen(this);
    }

    public void StopTime()
    {
        Time.timeScale = 0;
    }
    public void rRestartTime()
    {
        GameManager.GM.ResetTimeScale();
    }
    public void rPlayUIClip()
    {
        GameManager.GM.PlayClip(3);
    }

    public void LookAtMiddleOfBoard(){
        int mid = myBoard.width / 2;
        Transform midGem = myBoard.transform;
        foreach (Gem g in myBoard.GetComponentsInChildren<Gem>())
        {
            if (g.column == mid && g.row == mid)
            {
                midGem = g.transform;
            }
        }
        Vector3 lookPos = midGem.position - transform.position;
        lookPos.y = 0;
        Quaternion rot = Quaternion.LookRotation(lookPos);
        transform.parent.rotation = rot;
        //transform.parent.LookAt(midGem);
        //transform.parent.rotation = Quaternion.Euler(0, transform.parent.rotation.y, transform.parent.rotation.z);
    }

    public void UpdateBoundAlpha()
    {
        
        //print(Vector3.Distance(transform.position, myBoard.boundGrid.transform.position));
        float dist = Vector3.Distance(transform.position, myBoard.boundGrid.transform.position);
        float alpha = myBoard.boundGrid.material.color.a;
        Color c = myBoard.boundGrid.material.color;
        myBoard.boundGrid.material.color = new Color(c.r, c.g, c.b, 1 / dist);
        print(1 / dist);
    }

    public void DisallowClickThrough()
    {
        if(!DeviceIsNotNull() && inShop)
        {
            
            if (m_events.currentSelectedGameObject == null)
            {
               // print("calling disallow");

                m_events.SetSelectedGameObject(lastSelected);
            }
            else
            {
                lastSelected = m_events.currentSelectedGameObject;
            }
        }
    }


    //from: https://medium.com/slamatron/how-to-smoothly-rotate-an-object-over-time-with-a-single-button-click-event-in-unity3d-46d39b3785df
    //public IEnumerator Rotate(Vector3 axis, float angle, float duration = .5f)
    //{
    //    //i feel weird turning off the fps controller but let's see how it goes:
    //    GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
    //    Quaternion from = transform.parent.rotation;
    //    Quaternion to = transform.parent.rotation;

    //    to *= Quaternion.Euler(axis * angle);

    //    float elapsed = 0f;

    //    while(elapsed < duration && canRotate)
    //    {
    //        transform.parent.rotation = Quaternion.Slerp(from, to, elapsed / duration);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }
    //    GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = true;

    //    transform.rotation = to;
    //    canRotate = false;

    //}
    public IEnumerator ZoomCoolDown()
    {
        canZoom = false;
        yield return new WaitForSeconds(zoomCD);
        canZoom = true;
    }

}
