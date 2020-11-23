/*Attributions
    :miner by Luis Prado from the Noun Project
    :Voxel spaceships pack by Max Parata

    Sci-Fi UI - Lynda Mc Donald
    https://loudeyes.itch.io/sci-fi-ui-asset-pack-for-games

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiplayerWithBindingsExample;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using Steamworks;

public class GameManager : MonoBehaviour
{
    public static GameManager GM;
    public bool quickTest;
    public bool playingGame;
    public int[] colorsToSpawn;
    public int howManyInReserve;
    public Material[] possibleColors;
    public Color[] actualColors;
    public Material blindColor; //for the event that turns colors off.

    public TeamBuffs[] teamBuffs;
    public int[] teamBolts = new int[2]; //bolts are going to be added to and spent from here for now.
    public int[] boltsOnField = new int[2];
    public Vector2Int[] resolutions;
    int resolutionIndex;
    public Toggle fsToggle;
    public Text resText;

    [Header("Game States")]
    public bool onTitleScreen = true;
    public bool onMenuScreen = false;
    public bool onJoinScreen = false;
    public bool onStatsScreen = false;
    public bool everythingPause = false;
    public bool gameIsEnded;
    public bool showingIntro;
    private GameObject camRef; //this is held onto when the cinematic cam turns on; helps turn it off.

    public AudioClip[] sfxClips;
    public AudioClip[] minionDeathClips;
    public AudioClip[] matchAnnounceClips; //keep in line with matchHowMany (ex. [4] = tetramatch)
    public AudioListener ears;

    [Header("Game Variations")]
    public int gameMode;
    public bool autoMinions = true;
    public bool infiniteSwap,  itemsOn, eventsOn, 
    skillsOn, towersOn, turboMode, minionDiff, hatsOn,
    introOn, bigGameHunters, announcerOn, immortalMiners;


    //these don't save in PP atm (11 aug 20)
    [Header("Variation Vars")]
    public float eventInterval; //events will activate every x seconds.
    public float eventDuration; //events will last x seconds long.
    public float autoMinionInterval = 30f; //autominions will spawn every x seconds.
    public int startingBolts = 10;
    public int coreStartHealth = 1000;


    [Header("Game Options")]
    public bool noMatchesInitial = true;
    public float masterVol, musicVol, sfxVol;
    private AudioSource audSource;
    public AudioClip[] songs; //0=intro; 1=title; 2=ingame.
    private bool onlyOnce;
    [Tooltip("0=master; 1=music; 2=sfx")]
    public Slider[] volSliders;
    public Slider coreStart, boltStart;
    float tempMaster;
    public GameObject allTogglesParent, allEventsParent; //put the content from options panel here.
    public bool firstOpen = true;

    [System.Serializable]
    public class GemFrenzyInfo
    {
        public string lengthDesc;
        public int min;
        public int max;
    }
    public GemFrenzyInfo[] gFrenz;
    public int[] gFRandVals;
    public bool[] gFOn; //this is checked on and off by players...?

    //**********Steam Stuff***********//
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;

    [Header("Challenge Mode Tools")]
    //just a test, will probably do it another way, but
    public int[] challengeBoard;
    public ChallengeBoard[] cbs;
    public int cbIndex;
    public Image paintEmote;
    public Sprite paint, destroy;
    public bool challengeMaking, painting, destroying;
    public Text challengeName;


    public delegate void Callback();

    private void Awake()
    {
        GM = this;
        colorsToSpawn = new int[howManyInReserve];
        for(int i=0; i<howManyInReserve-1; i++)
        {
            colorsToSpawn[i] = Random.Range(0, possibleColors.Length);
        }
        if(challengeMaking)
        {
            CopyChallengeBoard();
            //challengeBoard = cbs[cbIndex].boardState;
            painting = true;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        int ind = PlayerPrefs.GetInt("resolutionIndex");
        if(!PlayerPrefs.HasKey("fullScreen"))
        {
            if(Application.platform !=RuntimePlatform.OSXPlayer)
            {
                PlayerPrefsX.SetBool("fullScreen", true);
            }
        }
        ChangeResolution(ind, PlayerPrefsX.GetBool("fullScreen", true));

        for(int i=0; i<teamBolts.Length; i++)
        {
            teamBolts[i] = startingBolts;
        }
        ears = FindObjectOfType<AudioListener>();//there should only be one!
        audSource = GetComponent<AudioSource>();
        audSource.clip = songs[0];
        
        
        //get saved audio options:
        if(!challengeMaking)GetSounds();
        firstOpen = PlayerPrefsX.GetBool("firstOpen", true);
        if(firstOpen)
        {
            DefaultOptions();
            PlayerPrefsX.SetBool("firstOpen", false);
        }
        GetOptions();
        
        //
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            //UIManager.UIM.EnableEndGame(1);
            //quickTest = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            //UIManager.UIM.EnableEndGame(0);
            //quickTest = true;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            //foreach(ShipCrash s in FindObjectsOfType<ShipCrash>())
            //{
            //    s.crashIntoMe = true;
            //}
        }
        //this is a little silly sitting here but let's see if it works...
        //sure does, time for lunch.
        if(onStatsScreen && (InControl.InputManager.ActiveDevice.RightBumper.WasPressed|| InControl.InputManager.ActiveDevice.LeftBumper.WasPressed))
        {
            UIManager.UIM.ChangePage();
        }
        MuteLogic();

        ChallengeLogic();
    }

    /// <summary>
    /// 0=bolt mined
    /// 1=bolt paid
    /// 2=upgrade complete
    /// 3=ui tone
    /// 4=zug zug
    /// 5=chop
    /// 6=not enough minerals
    /// </summary>
    public void PlayClip(int which){
        AudioSource.PlayClipAtPoint(sfxClips[which], ears.transform.position, SFXVol()); //not sure how well this will work with 2p.
    }
    public void PlayClip(int which, float vol){
        AudioSource.PlayClipAtPoint(sfxClips[which], ears.transform.position, vol* SFXVol()); //not sure how well this will work with 2p.
    }
    public void PlayClip(AudioClip which, float vol){
        AudioSource.PlayClipAtPoint(which, ears.transform.position, vol*SFXVol()); //not sure how well this will work with 2p.
    }
    public IEnumerator PlayClip(int whatClip, int howManyTimes, float delay)
    {
        for (int i = howManyTimes; i > 0; i--)
        {
            AudioSource.PlayClipAtPoint(sfxClips[whatClip], ears.transform.position,SFXVol()); 
            yield return new WaitForSeconds(delay);
        }
    }
    public IEnumerator PlayClipOnDelay(AudioClip whatClip, int howManyTimes, float delay)
    {
        yield return new WaitForSeconds(delay);

        AudioSource.PlayClipAtPoint(whatClip, ears.transform.position, SFXVol()*2);

    }

    public void PlayRandomDeathClip(){
        int i = Random.Range(0, minionDeathClips.Length);
        PlayClip(minionDeathClips[i],.2f);
    }

    /// <summary>
    /// strings = their bools
    /// </summary>
    /// <param name="variant"></param>
    public void GameVariantToggle(Toggle t)
    {
        string variant = t.name;
        switch (variant)
        {
            case "autoMinions":
                autoMinions = t.isOn;
                break;
            case "itemsOn":
                itemsOn = t.isOn;
                break;
            case "eventsOn":
                eventsOn = t.isOn;
                break;
            case "turboMode":
                turboMode = t.isOn;
                break;
            case "towersOn":
                towersOn = t.isOn;
                break;
            case "skillsOn":
                skillsOn = t.isOn;
                break;
            case "infiniteSwap":
                infiniteSwap = t.isOn;
                break;
            case "minionDifferences":
                minionDiff = t.isOn;
                break;
            case "hatsOn":
                hatsOn = t.isOn;
                break;
            case "introOn":
                introOn = t.isOn;
                break;
            case "fullScreen": //not storing this in a bool, just using pp for now.
                ChangeResolution(resolutionIndex, !PlayerPrefsX.GetBool("fullScreen")); //using opposite because it gets changed in like 4 lines
                break;
            case "bigGameHunters":
                bigGameHunters = t.isOn;
                break;
            case "announcerOn":
                announcerOn = t.isOn;
                break;

        }
        //also save the options for next time:
        SetOption(variant, t.isOn);
        //!!note, not Getting them just yet...
    }

    public void ChangeEventInterval(Text tToMod)
    {
        if (eventInterval >= 180)
        {
            eventInterval = 30;
        }
        else
        {
            eventInterval += 10;
        }
        tToMod.text = "(Every " + eventInterval + " seconds)";

    }

    public void ChangeEventDur(Text tToMod)
    {
        if (eventDuration >= 60)
        {
            eventDuration = 10;
        }
        else
        {
            eventDuration += 5;
        }
        tToMod.text = "(Lasts " + eventDuration + " seconds)";

    }

    public void ChangeAutoMinInterval(Text tToMod)
    {
        if (autoMinionInterval >= 180)
        {
            autoMinionInterval = 10;
        }
        else
        {
            autoMinionInterval += 5;
        }
        tToMod.text = "(Every " + autoMinionInterval + " seconds)";

    }



    public void ChangeCoreStart(Text tToMod)
    {
        coreStartHealth = (int)coreStart.value*500;
        tToMod.text = "Core Start Health ("+coreStartHealth+")";
        ObjectsManager.instance.bCore.SetMaxHealth(coreStartHealth);
        ObjectsManager.instance.rCore.SetMaxHealth(coreStartHealth);
        UIManager.UIM.SetTexts();


    }

    public void ChangeBoltStart(Text tToMod)
    {
        startingBolts = (int)boltStart.value*10;
        tToMod.text = "Starting Bolts ("+ startingBolts + ")";
        teamBolts[0] = startingBolts;
        teamBolts[1] = startingBolts;
        UIManager.UIM.SetTexts();

    }

    /// <summary>
    /// 0=title screen; 1=join screen; 2=playingGame; 3=menuscreen; 4=statsScreen
    /// </summary>
    /// <param name="i"></param>
    public void SetGameState(int i)
    {
        //this could be done much neater, code-wise, but eh:
        switch (i)
        {
            case 0:
                onTitleScreen = true;
                onJoinScreen = false;
                playingGame = false;
                onMenuScreen = false;
                onStatsScreen = false;

                break;

            case 1:
                onTitleScreen = false;
                onJoinScreen = true;
                playingGame = false;
                onMenuScreen = false;
                onStatsScreen = false;
                break;

            case 2:
                onTitleScreen = false;
                onJoinScreen = false;
                playingGame = true;
                onMenuScreen = false;
                onStatsScreen = false;
                showingIntro = false;
                StartCoroutine(LoadSong(0f, songs[2], true));
                break;
            case 3:
                onTitleScreen = false;
                onJoinScreen = false;
                playingGame = false;
                onMenuScreen = true;
                onStatsScreen = false;

                break;
            case 4:
                onTitleScreen = false;
                onJoinScreen = false;
                playingGame = false;
                onMenuScreen = false;
                onStatsScreen = true;
                showingIntro = false;


                break;
            case 5:
                showingIntro = true;
                onJoinScreen = false;

                break;
        }
    }

    //!!this is being called at Start from the slider's OnValueChanged.
    //Moved the call for this to EventTrigger > Move
    public void UpdateSounds()
    {

        masterVol = volSliders[0].value;
        musicVol = volSliders[1].value;
        sfxVol = volSliders[2].value;
        if(onJoinScreen)
        masterVol = volSliders[3].value;

        SetSounds();
    }
    void SetSounds()
    {
        //set them in pp:
        PlayerPrefs.SetFloat("masterVol", masterVol);
        PlayerPrefs.SetFloat("musicVol", musicVol);
        PlayerPrefs.SetFloat("sfxVol", sfxVol);
        GetSounds();
    }

    void GetSounds()
    {
        masterVol = PlayerPrefs.GetFloat("masterVol",1);
        musicVol = PlayerPrefs.GetFloat("musicVol",1);
        sfxVol = PlayerPrefs.GetFloat("sfxVol",1);
        audSource.volume = musicVol*masterVol;


        volSliders[0].value = masterVol;
        volSliders[1].value = musicVol;
        volSliders[2].value = sfxVol;
        if(onJoinScreen)
        {
            volSliders[3].value = masterVol;

        }

    }

    void MuteLogic()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (volSliders[0].value != 0)
            {
                tempMaster = volSliders[0].value;
                volSliders[0].value = 0;
            }
            else
            {
                volSliders[0].value = tempMaster;
            }


        }
        
        if(!challengeMaking)UpdateSounds();
    }

    public void ToggleBuffHints(Toggle to)
    {
        foreach(PlayerShoot p in PlayerManager.PM.players)
        {
            p.GemBuffOption(to);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ReloadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(0);
    }

    public void SyncVolSliders(){
        volSliders[3].value = volSliders[0].value;

    }

    public void PauseGame(bool trueMeansYes)
    {
        everythingPause = trueMeansYes;
        //!not sure if we should set timescale to zero here or not.
    }

    ///// <summary>
    ///// Calculates the diff.
    ///// </summary>
    ///// <returns>The diff.</returns>
    ///// <param name="a">The part.</param>
    ///// <param name="b">whole.</param>
    //public float CalcDiff(float a, float b)
    //{
    //    return (a - b);
    //}

    //public void CancelTheInvokes()
    //{
    //    ObjectsManager.instance.CancelInvoke("SpawnDownAllLanes");

    //    EventsManager.EM.CancelInvoke("ChooseACard");


    //}

    //public void StartTheInvokes()
    //{
    //    //math time.
    //    float n_event = eventInterval-StatsManager.SM.gameTime;
    //    float n_auto = autoMinionInterval;
    //}

    public float GetClipTime(Animator anim, string aName)
    {
        float time = 0;
        foreach (AnimationClip c in anim.runtimeAnimatorController.animationClips)
        {
            if (c.name == aName)
            {
                time = c.length;
            }
        }
        return time;
    }

    //moving this to a function (used to be all done through animation clips) to help with checks:
    public void ShowCinematic(GameObject camToTurnOn)
    {
        if(introOn){
            camRef = camToTurnOn;
            camToTurnOn.SetActive(true);
            SetGameState(5); //turns on showingIntro
        }else{
            if(camRef)
            camRef.SetActive(false);
            PlayerManager.PM.SetAllFPSControllers(true);
        }
    }

    public void SetAutoPlayDifficulty()
    {
        Board b = null;
        foreach(Board bb in FindObjectsOfType<Board>())
        {
            if(bb.autoPlay)
            {
                b = bb; //amazing.
            }
        }
        switch(b.difficulty)
        {
            //double speed for destroying gems.
            case 2:
            case 3:
                b.checkDelay /= 2;
                b.refillDelay /= 2;
                b.backToPlayDelay /= 2;
                //print("board starts with green researcher");
                StartCoroutine(ObjectsManager.instance.SpawnEnemy(ObjectsManager.instance.boardsInplay[1], 2, b.myPaths[3], 3, 1, 3));
                ObjectsManager.instance.rCore.SetMaxHealth((int)ObjectsManager.instance.rCore.mxHealth * b.difficulty);

                break;
        }
    }

    //****PlayerPrefs Work*****//

    [Button]
    public void InitializeAllOptions(){

        PlayerPrefsX.SetBool("infiniteSwap", infiniteSwap);
        PlayerPrefsX.SetBool("itemsOn", itemsOn);
        PlayerPrefsX.SetBool("eventsOn", eventsOn);
        PlayerPrefsX.SetBool("skillsOn", skillsOn);
        PlayerPrefsX.SetBool("towersOn", towersOn);
        PlayerPrefsX.SetBool("turboMode", turboMode);
        PlayerPrefsX.SetBool("minionDiff", minionDiff);
        PlayerPrefsX.SetBool("hatsOn", hatsOn);
        PlayerPrefsX.SetBool("introOn", introOn);
        PlayerPrefsX.SetBool("autoMinions", autoMinions);
        PlayerPrefsX.SetBool("bigGameHunters", bigGameHunters);
        PlayerPrefsX.SetBool("immortalMiners", immortalMiners);
        PlayerPrefs.SetInt("resolutionIndex", resolutionIndex);
        
        SetAllEventsOn();
        CheckEvents();
    }

    [Button]
    public void DefaultOptions(){
        PlayerPrefsX.SetBool("autoMinions", true);

        PlayerPrefsX.SetBool("infiniteSwap", false);
        PlayerPrefsX.SetBool("itemsOn", true);
        PlayerPrefsX.SetBool("eventsOn", false);
        PlayerPrefsX.SetBool("skillsOn", false);
        PlayerPrefsX.SetBool("towersOn", true);
        PlayerPrefsX.SetBool("turboMode", false);
        PlayerPrefsX.SetBool("minionDiff", true);
        PlayerPrefsX.SetBool("hatsOn", false);
        PlayerPrefsX.SetBool("introOn", true);
        PlayerPrefsX.SetBool("bigGameHunters", false);
        PlayerPrefsX.SetBool("announcerOn", true);
        PlayerPrefsX.SetBool("immortalMiners", true);
        PlayerPrefs.SetFloat("masterVol", .5f);
        PlayerPrefs.SetFloat("sfxVol", .5f);
        PlayerPrefs.SetFloat("musicVol", .5f);

        GetSounds();
        foreach (Toggle t in allTogglesParent.GetComponentsInChildren<Toggle>()){
            t.gameObject.SetActive(false);
            t.gameObject.SetActive(true); //this should trigger the ToggleCheck OnEnable() function.

        }

        SetAllEventsOn();
        CheckEvents();
        //!this is not reliably working...
        Invoke("RidiculousDelay", 1f);

    }

    public void RidiculousDelay()
    {
        print("resetting evennnnts");
        EventsManager.EM.CheckAddAllEvents();
    }

    public void SetAllEventsOn()
    {
        foreach(Toggle t in allEventsParent.GetComponentsInChildren<Toggle>())
        {
            PlayerPrefsX.SetBool(t.gameObject.name, true);
            t.gameObject.SetActive(false);
            t.gameObject.SetActive(true); //this should trigger the ToggleCheck OnEnable() function.

        }

    }

    //!this is a little scary because it relies on names(strings)
    public void CheckEvents()
    {
        foreach (Toggle t in allEventsParent.GetComponentsInChildren<Toggle>())
        {
            t.isOn = PlayerPrefsX.GetBool(t.gameObject.name);
            t.gameObject.SetActive(false);
            t.gameObject.SetActive(true); //this should trigger the ToggleCheck OnEnable() function.



        }
    }

    public void SetOption(string which, bool state)
    {
        PlayerPrefsX.SetBool(which, state);
    }

    public void GetOptions()
    {
        introOn = PlayerPrefsX.GetBool("introOn", true);
        infiniteSwap = PlayerPrefsX.GetBool("infiniteSwap", false);
        itemsOn = PlayerPrefsX.GetBool("itemsOn");
        eventsOn = PlayerPrefsX.GetBool("eventsOn", false);
        skillsOn = PlayerPrefsX.GetBool("skillsOn");
        towersOn = PlayerPrefsX.GetBool("towersOn");
        turboMode = PlayerPrefsX.GetBool("turboMode");
        minionDiff = PlayerPrefsX.GetBool("minionDiff");
        hatsOn = PlayerPrefsX.GetBool("hatsOn");
        introOn = PlayerPrefsX.GetBool("introOn");
        autoMinions = PlayerPrefsX.GetBool("autoMinions");
        resolutionIndex = PlayerPrefs.GetInt("resolutionIndex");
        bigGameHunters = PlayerPrefsX.GetBool("bigGameHunters");
        announcerOn = PlayerPrefsX.GetBool("announcerOn");
        immortalMiners = PlayerPrefsX.GetBool("immortalMiners", true);

    }
    public void GetOption(Toggle t){
        t.isOn = PlayerPrefsX.GetBool(t.name);
    }

    //*****STEAM STUFF****//
    public void OnEnable()
    {
        if (SteamManager.Initialized)
        {
            m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
        }
    }

    private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
    {
        if(pCallback.m_bActive != 0)
        {
            Debug.Log("Steam Overlay has been activated");
            PauseGame(true);
        }
        else
        {
            Debug.Log("Steam Overlay has been closed");
            PauseGame(false);
        }
    }

    public float SFXVol()
    {
        float vol = 1;
        float playerMod = 1;
        if (PlayerManager.PM.players.Count>0)
        {
            playerMod = PlayerManager.PM.players.Count;
        }
        vol = (sfxVol * masterVol) / playerMod;
        //print(vol);
        return vol;
    }

    public void NavResolutions(int one)
    {
        if(resolutionIndex<resolutions.Length-1)
        {
            resolutionIndex += one;
        }else{
            resolutionIndex =0;
        }
        PlayerPrefs.SetInt("resolutionIndex", resolutionIndex);
        ChangeResolution(resolutionIndex, PlayerPrefsX.GetBool("fullScreen"));
    }

    //public void ToggleFS()
    //{

    //    fsToggle.isOn = !fsToggle.isOn;
    //    PlayerPrefsX.SetBool("fullScreen", fsToggle.isOn);
    //}

    public void ChangeResolution(int index, bool full)
    {
        Screen.SetResolution(resolutions[index].x, resolutions[index].y, full);
        resText.text = resolutions[index].x + "x" + resolutions[index].y;
    }

    public void ChallengeLogic()
    {
        if(challengeMaking)
        {
            if(Input.GetKeyDown(KeyCode.Tab))
            {
                if(painting)
                {
                    painting = false;
                    destroying = true;
                    paintEmote.sprite = destroy;
                }else{
                    destroying = false;
                    painting = true;
                    paintEmote.sprite = paint;
                }

            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

        }
    }

    public void ChallengeIndex(int c)
    {
        //for now this is not carrying over remainders or anything.
        //if you go over the limit, you wrap back to 0.
        int newIndex = cbIndex + c;
        if(newIndex>=cbs.Length)
        {
            newIndex = 0;
        }else if(newIndex<0)
        {
            newIndex = cbs.Length;
        }
        cbIndex = newIndex;
        CopyChallengeBoard();
        challengeName.text = cbIndex.ToString() + ": " +cbs[cbIndex].name;

        //also update the board?
        if(FindObjectOfType<Board>())
        FindObjectOfType<Board>().ChallengeUpdate();
    }

    private void CopyChallengeBoard()
    {
        for (int i = 0; i < challengeBoard.Length; i++)
        {
            challengeBoard[i] = cbs[cbIndex].boardState[i];
        }
    }

    //dropping it here so an Animation Event can trigger it.
    public void LoadMenuSong()
    {
        

        if (!onlyOnce)
        {
            StartCoroutine(LoadSong(audSource.clip.length, songs[1], true));
            onlyOnce = true;
        }
    }

    public IEnumerator LoadSong(float delay, AudioClip clip, bool loopIt)
    {
        yield return new WaitForSeconds(delay);
        audSource.enabled = true;
        yield return new WaitForSeconds(delay);

        audSource.clip = clip;
        audSource.Play();
        audSource.loop = loopIt;
    }

    [Button]
    public void SetFirstOpenTrue()
    {
        PlayerPrefsX.SetBool("firstOpen", true);
    }

    public IEnumerator TurnOffBool(Callback callback, float delay)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();

    }

    public void ResetTimeScale()
    {
        if (turboMode)
        {
            Time.timeScale = 2;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    /// <summary>
    /// 0=vs; 1=gem frenzy; 2=coop
    /// </summary>
    /// <param name="what"></param>
    public void SetGameMode(int what)
    {
        gameMode = what;
        //turn certain things on/off according to what is sent in here, probably?
    }

    public void GenerateRandomGemVals(int sliderVal)
    {

        for (int i = 0; i < gFRandVals.Length; i++)
        {
            if(gFOn[i]){
                gFRandVals[i] = Random.Range(gFrenz[sliderVal].min, gFrenz[sliderVal].max);
            }

        }
    }

    public void ToggleGemFrenz(int index){
        //this should be fine if they all start on by default.
        gFOn[index] = !gFOn[index];
        if(!gFOn[index]){
            //when falsified, it should set the max val to 0 and enable the checkmark:
            gFRandVals[index] = 0;
            //UIManager.UIM.gFrenzHelpers[0].checkmarks[index].enabled = true;
            //UIManager.UIM.gFrenzHelpers[1].checkmarks[index].enabled = true;

        }
    }


}

