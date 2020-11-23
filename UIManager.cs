using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InControl;
using NaughtyAttributes;
using MultiplayerWithBindingsExample;
using Steamworks;

public class UIManager : MonoBehaviour
{
    
    public static UIManager UIM;
    public string[] playerNames;
    public GameObject joinMenu;

    public Sprite[] buffIcons; //what buffs this color minion? //also good for colorblindness?
    private PlayerShoot pQuitting;


    [Header("Game Options Menu")]
    public GameObject gOptionsMenu;
    public bool inGameOptions;
    public List<GameObject> tempOffMods = new List<GameObject>();
    public Selectable optionsFirstSelect;
    public Button gameOptions;
    int controlsMapIndex; //0=gamepad, 1=kbam
    public Image controlsImage;
    public Sprite[] controls;


    [Header("Game Info")]
    public Canvas gameInfoCan;
    public Text[] coreHealthText,teamBoltsText, teamTowerKills, pNameTs, pInGameNameTexts, teamResearchText, teamItemsText;
    public Text gameTime, endGameMessage;
    public GameObject endGame, quitNotification, disconnect, howtoScreen, introCanvas, AIDiffScreen, gemFrenzyPanel;
    public Image introSkipCircle;
    public bool inHowTo;
    //public string[] gFrenzyMatchDesc;
    public Text gFrenzyLengthText;
    public Slider gFrenzySlid;
    public GameObject[] vsPanels, frenzyPanels;
    public GameObject timerBG;
    public List<HealthbarFill> bGemFills, rGemFills;
    public GemFrenzyHelper[] gFrenzHelpers; //these will be where to access the checkmarks and text elements per color.
    public GameObject slotGFrenzy;

    [Header("End Game UI")]
    [Tooltip("in order of inspector")]
    public Text[] p1Texts;
        public Text[] p2Texts;
    public Text[] pNames;
    public GameObject[] endGameCams;
    Coroutine optionsToggleRef;

    [Header("Stats Screen UI")]
    public GameObject[] statsPages;
    public Text[] statTexts;

    [Header("Cams")]
    public GameObject[] introCams;

    private void Awake()
    {
        UIM = this;
        playerNames = new string[4];

    }
    // Start is called before the first frame update
    void Start()
    {
        endGame.SetActive(false);
        //2 for now since there should only ever really be two teams.
        coreHealthText = new Text[2];
        teamBoltsText = new Text[2];
        teamTowerKills = new Text[2];
        teamResearchText = new Text[2];
        teamItemsText = new Text[2];
        pInGameNameTexts = new Text[2];
        pNameTs = new Text[2];

        //teamTowerKills = new Text[MultiplayerWithBindingsExample.PlayerManager.PM.players.Count];
        foreach (Text t in gameInfoCan.GetComponentsInChildren<Text>(true))
        {
            switch (t.name)
            {
                case "BlueCoreText":
                    coreHealthText[0] = t;
                    break;
                case "RedCoreText":
                    coreHealthText[1] = t;
                    break;

                case "BBoltsText":
                    teamBoltsText[0] = t;
                    break;
                case "RBoltsText":
                    teamBoltsText[1] = t;
                    break;

                case "BResearchText":
                    teamResearchText[0] = t;
                    break;
                case "RResearchText":
                    teamResearchText[1] = t;
                    break;

                case "BItemsText":
                    teamItemsText[0] = t;
                    break;
                case "RItemsText":
                    teamItemsText[1] = t;
                    break;
                case "BlueKills":
                    teamTowerKills[1] = t; //opposite numbering convention cause easier to add kills this way
                    break;
                case "RedKills":
                    teamTowerKills[0] = t;
                    break;

                case "P1Name":
                    pNameTs[0] = t;
                    break;
                case "P2Name":
                    pNameTs[1] = t;
                    break;

                case "IGPlayerName0":
                    pInGameNameTexts[0] = t;
                    break;
                case "IGPlayerName1":
                    pInGameNameTexts[1] = t;
                    break;

                case "EGMessage":
                    endGameMessage = t;
                    break;

                case "GTText":
                    gameTime = t;
                    break;
            }
        }

        foreach(HealthbarFill ig in frenzyPanels[0].GetComponentsInChildren<HealthbarFill>())
        {
            bGemFills.Add(ig);
        }
        foreach (HealthbarFill ig in frenzyPanels[1].GetComponentsInChildren<HealthbarFill>())
        {
            rGemFills.Add(ig);
        }

        InitStats();
    }
    void InitStats()
    {
        SetTexts();
        gameInfoCan.gameObject.SetActive(false);
    }

    public void SetTexts()
    {
        for (int i = 0; i < 2; i++)
        {
            teamBoltsText[i].text = GameManager.GM.startingBolts.ToString();
            teamTowerKills[i].text = 0.ToString();
        }
        coreHealthText[0].text = ObjectsManager.instance.bCore.health.ToString();
        coreHealthText[1].text = ObjectsManager.instance.rCore.health.ToString();

    }

    public string SteamName()
    {
        if (SteamManager.Initialized)
        {
            string sName = SteamFriends.GetPersonaName();
            return sName;
        }else{
            return "Fancy Pants";
        }
    }

    

    // Update is called once per frame
    void Update()
    {
     
    }

    [Button]
    public void RedTeamWins()
    {
        //endGameMessage.text = "Sit down, " + playerNames[0] + ", " + playerNames[1] + " is the winner!";
        //endGameMessage.color = Color.red;
        endGame.SetActive(true);
        SetEndGameStats();
        TurnOnPlayerTrophers(1);
        GameManager.GM.gameIsEnded = true;

    }

    [Button]
    public void BlueTeamWins()
    {
        //endGameMessage.text = "Sit down, " + playerNames[1] + ", " + playerNames[0] + " is the winner!";
        //endGameMessage.color = Color.blue;
        SetEndGameStats();
        endGame.SetActive(true);
        TurnOnPlayerTrophers(0);
        GameManager.GM.gameIsEnded = true;

    }

    public void EnableEndGame(int losingTeam)
    {
        GameManager.GM.gameIsEnded = true;
        GameManager.GM.playingGame = false;
        if (Time.timeScale != 1) //reset turbo mode at this point.
        {
            Time.timeScale = 1;
        }
        if (losingTeam == 0)
        {
            //endGameMessage.text = "Sit down, " + playerNames[losingTeam] + ", " + playerNames[1] + " is the winner!";
            endGameMessage.color = Color.red;
            if (PlayerManager.PM.players.Count > 1)
            {
                endGameMessage.text = playerNames[1] + " WINS!";
            }
            else
            {
                endGameMessage.text = "ROBOTO WINS!";
            }
            endGameCams[1].SetActive(true);
            TurnOnPlayerTrophers(1);

        }
        else
        {
            //endGameMessage.text = "Sit down, " + playerNames[losingTeam] + ", " + playerNames[0] + " is the winner!";
            endGameMessage.color = Color.blue;
            endGameMessage.text = playerNames[0] + " WINS!";

            endGameCams[0].SetActive(true);

            TurnOnPlayerTrophers(0);
        }
        
        SetEndGameStats();
        endGame.SetActive(true);
    
    }
    public void InvokeGameOptions(EventSystem es){

        gOptionsMenu.GetComponentInChildren<UIScrollToSelectionXY>().events = es;
        if(optionsToggleRef!=null)
        StopCoroutine(optionsToggleRef);


        gameOptions.onClick.Invoke();

        //ugh...
        es.SetSelectedGameObject(GameObject.Find("VolumeSlider"));

        inGameOptions = true;
        
    }
    //this is for button pressing... not sure why
    //I set it up this way.
    public void ToggleInGameOptions(bool trueOrFalse)
    {
        inGameOptions = trueOrFalse;
    }
    //a button will call this.
    public void BackOutOfOptions()
    {
        //StartCoroutine(GameManager.GM.TurnOffBool(() => inGameOptions = true, 1f));
        foreach (MyEventSystem mEvent in PlayerManager.PM.controllerEvents)
        {
            mEvent.SetSelectedGameObject(mEvent.playerPanelFirst);
        }
        foreach (MyEventSystem kbEvent in PlayerManager.PM.kbEventSystems)
        {
            kbEvent.SetSelectedGameObject(kbEvent.playerPanelFirst);
        }
        //turn on the other players' navigation capabilities again.
        foreach (GameObject g in tempOffMods)
        {
            g.SetActive(true);
        }
        optionsToggleRef = StartCoroutine(GameManager.GM.TurnOffBool(() => inGameOptions = false, .1f));
    }

    //I've moved the following functionality to a button in the Inspector (OptionsButton)
    public void EnterOptions(bool inOrFalse, PlayerShoot whosGoingIn)
    {
        joinMenu.SetActive(!inOrFalse);
        inGameOptions = inOrFalse;
        if (inOrFalse)
        {
            
            gOptionsMenu.SetActive(true);
            gOptionsMenu.GetComponent<Animator>().ResetTrigger("PaneOut");

            gOptionsMenu.GetComponent<Animator>().SetTrigger("PaneIn");
            whosGoingIn.m_events.SetSelectedGameObject(optionsFirstSelect.gameObject);
        }
        else
        {
            gOptionsMenu.GetComponent<Animator>().ResetTrigger("PaneIn");

            gOptionsMenu.GetComponent<Animator>().SetTrigger("PaneOut");
            whosGoingIn.m_events.SetSelectedGameObject(whosGoingIn.m_events.playerPanelFirst);
            //turn on the other players' navigation capabilities again.
            foreach(GameObject g in tempOffMods)
            {
                g.SetActive(true);
            }
        }
    }

    [Button]
    public void SetEndGameStats()
    {
        //for(int i=0; i<1; i++)
        //{
        for (int j = 0; j < p1Texts.Length; j++)
        {
            p1Texts[j].text = StatsManager.SM.pStats[0, j].ToString();
            p2Texts[j].text = StatsManager.SM.pStats[1, j].ToString();

            //love it, for the stars to highlight who had more:
            if(StatsManager.SM.pStats[0,j]> StatsManager.SM.pStats[1, j])
            {
                p1Texts[j].GetComponentInChildren<Image>(true).gameObject.SetActive(true);
            }
            else if (StatsManager.SM.pStats[0, j] < StatsManager.SM.pStats[1, j])
            {
                p2Texts[j].GetComponentInChildren<Image>(true).gameObject.SetActive(true);

            }

            //print(p1Texts[j].name + " " + StatsManager.SM.pStats[i, j].ToString() + "\n");
        }
        //add stats to account
        for(int i=0; i<PlayerManager.PM.players.Count; i++)
        {
            StatsManager.SM.AddRoundStatsToAccount(PlayerManager.PM.players[i].myPIN, PlayerManager.PM.players[i].playerNum);
        }

        //turn on names, too:
        pNames[0].text = playerNames[0];
        if(PlayerManager.PM.players.Count>1)
        {
            pNames[1].text = playerNames[1];
        }else{
            pNames[1].text = "ROBOTO";
        }


        //}
    }

    public void TurnOnPlayerTrophers(int which)
    {
        foreach(Image im in pNames[which].GetComponentsInChildren<Image>(true))
        {
            im.enabled = true;
        }
        //if there's a player in this index (which)
        if(PlayerManager.PM.players.Count>1){
            StatsManager.SM.IncrementWins(which); //then that player gets some win action.

        }else{
            StatsManager.SM.IncrementLosses(0);
        }
        GameManager.GM.everythingPause = true;
    }

    public void ChangePage()
    {
        int ind = 0;
        for (int i = 0; i < statsPages.Length; i++)
        {
            if (statsPages[i].activeSelf)
            {
                ind = i;
                break;
            }
        }
        print("stat page " + ind + " is open");


        if (ind<statsPages.Length-1)
        {
            statsPages[ind + 1].SetActive(true);
        }
        else
        {
            statsPages[0].SetActive(true);
        }
        statsPages[ind].SetActive(false);

    }

    public void QuitNotif(PlayerShoot p)
    {
        //enable r u sure notification:
        //GameManager.GM.everythingPause = true;
        //just using timescale for now.  seems to work okay and pauses the InvokeRepeating calls.
        Time.timeScale = 0;
        quitNotification.SetActive(true);
        pQuitting = p;
        //select the no button:
        foreach(Transform t in quitNotification.GetComponentsInChildren<Transform>())
        {
            if(t.name.Contains("No")){
                p.m_events.SetSelectedGameObject(t.gameObject);
            }
        }

    }
    public void ShowHowToOnScreen(PlayerShoot p)
    {
        howtoScreen.SetActive(true);
        howtoScreen.GetComponent<HowToHelper>().overrideE = p.m_events;
        p.m_events.SetSelectedGameObject(howtoScreen.GetComponent<HowToHelper>().myBack.gameObject);
        //howtoScreen.GetComponent<HowToHelper>().myBack.Select();
        inHowTo = true;
    }

    public void ContDiscon(bool trueForOn)
    {
        //enable r u sure notification:
        //GameManager.GM.everythingPause = true;
        //just using timescale for now.  seems to work okay and pauses the InvokeRepeating calls.
        //Time.timeScale = 0;
        disconnect.SetActive(trueForOn);

    }

    //just making this to close the options menu of the player who had it open.
    public void LeaveQuit()
    {
        GameManager.GM.ResetTimeScale();
        pQuitting.ToggleOptions(false);
    }
    public void LeaveHowTo()
    {
        inHowTo = false;
        foreach(PlayerShoot p in PlayerManager.PM.players)
        {
            p.inOptions = false;
        }
    }

    public void ChangeControls(int by){

        controlsMapIndex += by;

        if (controlsMapIndex > controls.Length-1)
        {
            controlsMapIndex = 0;
        }
        else if(controlsMapIndex<0)
        {
            controlsMapIndex = controls.Length - 1;
        }

        controlsImage.sprite = controls[controlsMapIndex];

        ////not sure if this is faster or fancier.
        //int h = 0;

        //foreach (GameObject g in controls)
        //{
        //    if(g.activeSelf)
        //    {
        //        h = System.Array.IndexOf(controls, g);
        //    }
        //}

    }
    public void SetControls()
    {
        if(PlayerManager.PM.kbEventSystems[0].isActiveAndEnabled){
            controlsImage.sprite = controls[1];
        }else{
            controlsImage.sprite = controls[0];
        }
    }

    public void SelectDelay(Selectable butt)
    {
        StartCoroutine(SelectDelayCR(butt.gameObject));
    }
    IEnumerator SelectDelayCR(GameObject but)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(but);
    }

    public void BeginGameButton(){
        //this is going to be based on game mode first:
        switch (GameManager.GM.gameMode)
        {
            case 0:
                if (PlayerManager.PM.players.Count == 1)
                {
                    //let the player choose an AI difficulty:
                    AIDiffScreen.SetActive(true);
                    //also set p2's name to roboto:
                    pInGameNameTexts[1].text = "ROBOTO :)";
                }
                else
                {
                    introCanvas.SetActive(false);
                    TurnOnRandomIntro();
                }
                break;

            case 1:
                gemFrenzyPanel.SetActive(true);
                break;
        }
        

        //turn off the items purcashed UI if that option is turned off:
        if(!GameManager.GM.itemsOn)
        {
            teamItemsText[0].gameObject.SetActive(false);
            teamItemsText[1].gameObject.SetActive(false);
        }
    }

    public void TurnOnRandomIntro()
    {
        introCams[Random.Range(0, introCams.Length)].SetActive(true);

    }
    public void GemFrenzyText()
    {
        gFrenzyLengthText.text = GameManager.GM.gFrenz[(int)gFrenzySlid.value].lengthDesc + " " + 
            GameManager.GM.gFrenz[(int)gFrenzySlid.value].min + "-" +
            GameManager.GM.gFrenz[(int)gFrenzySlid.value].max + " each";
        GameManager.GM.GenerateRandomGemVals((int)gFrenzySlid.value);

       
    }

    public void SetIGFrenzyGemTexts()
    {
        //init the UI for gemfrenzy
        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < GameManager.GM.actualColors.Length; j++)
            {
                gFrenzHelpers[i].progressTexts[j].text = "0/" + GameManager.GM.gFRandVals[j];
                //if the gem was turned off, turn on the checkmark.
                if(!GameManager.GM.gFOn[j])
                {
                    gFrenzHelpers[i].checkmarks[j].enabled = true;
                    FrenzyFill(i, j, 1);//also fill the image.
                }
            }
        }
    }


    public void FrenzyFill(int team, int color, int prog)
    {
        gFrenzHelpers[team].progressTexts[color].text = prog + "/" + GameManager.GM.gFRandVals[color];

        //calculate percentage complete:
        float p = (float)prog / GameManager.GM.gFRandVals[color];
        if (team == 0)
        {
            //print("filling " + color + " " + p);
            bGemFills[color].SetFill(p);
        }
        else
        {
            rGemFills[color].SetFill(p);
        }
    }

    public void TurnOnPanels(int mode)
    {
        vsPanels[0].SetActive(false);
        vsPanels[1].SetActive(false);
        frenzyPanels[0].SetActive(false);
        frenzyPanels[1].SetActive(false);
        //timerBG.SetActive(false);
        //option to turn off UI:
        if (mode!=-1)
        {
            //timerBG.SetActive(true);
            switch (GameManager.GM.gameMode)
            {
                case 0:
                    vsPanels[0].SetActive(true);
                    vsPanels[1].SetActive(true);
                    break;

                case 1:
                    frenzyPanels[0].SetActive(true);
                    frenzyPanels[1].SetActive(true);
                    SetIGFrenzyGemTexts();
                    break;
            }
        }
    }

    public IEnumerator SpinGFSlot(float totalTime)
    {
        GameManager.GM.SetGameState(5);//showing intro.

        //turn it on so it starts spinning:
        slotGFrenzy.SetActive(true);
        ShowRandomNumbers[] srn = slotGFrenzy.GetComponentsInChildren<ShowRandomNumbers>();

        //wait a sec, let it 'spin'
        yield return new WaitForSeconds(totalTime / 2);

        //then loop through and turn them off after a bit of a delay:
        for (int i = 0; i < srn.Length; i++)
        {
            srn[i].CancelInvoke();
            srn[i].enabled = false;
            //print("turning off " + i);
            //actually set the number:
            srn[i].txt.text = GameManager.GM.gFRandVals[i].ToString();
            //!probably also activate a UI effect or something:
            GameManager.GM.PlayClip(3);
            yield return new WaitForSeconds(totalTime / srn.Length);
        }
        yield return new WaitForSeconds(totalTime / 4);
        //call an anim to turn it off:
        slotGFrenzy.SetActive(false);

        GameManager.GM.SetGameState(2);//playing game.

    }

}
