using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using MultiplayerWithBindingsExample;

public class StatsManager : MonoBehaviour
{
    public static StatsManager SM;

    [Header("Overall Stats")]
    public float totalTimePlayed;

    [Header("Game Stats")]
    public float gameTime;
    [Tooltip("ex. P2 kills are in minionKills[2]")]
    public int[] minionKills,towerKills, minionsSpawned, researchLvl,
        itemsPurchased,peonsSpawned,boltsCollected,
        gemsDestroyed,totalSwaps, wins, losses, wins_p, losses_p, wins_ai, losses_ai; //array for players.
    [Tooltip("To access P1 Match7s, check for matchesMade[1,7]")]
    public int[,] matchesMade;

    [Header("Account Stats")]
    public string[] accountPrefs;

    [Header("ToShow Vars")]
    public List<Text> matchesTexts;
    public Queue<Text> fifo;
    public string tempPin;
    List<Text> turnOnList = new List<Text>();
    public Text[] colorSums, matchSums; //might be weird since other texts are in UIM
    public Text totalTotalTxt;
    public Text[] statScreenStatNums;

    [System.Serializable]
    public class Numbers
    {
        public string name;
        public Text[] matchNumbers;
    }
    public Numbers[] bigBoiFromOutkast;

    //[System.Serializable]
    //public class Stat
    //{
    //    public string statName; //not using it rn but if i want to change the names via code that'd be good...?
    //    public int statNum;
    //}
    public int[,] pStats;


    //*********STEAM STUFF******//
    public SteamStatsAndAchievements steamStats;
    [NaughtyAttributes.Button]
    public void ResetAllUserStats()
    {
        SteamUserStats.ResetAllStats(true);
        //SteamUserStats.ClearAchievement()
        ResetAllAchievements();
        for (int i = 3; i < steamStats.steamMatches.Length; i++)
        {
            for (int j = 10; j < 100; j+=10){
                PlayerPrefsX.SetBool("match_" + i + j, false);//might be a little weird that i'm doing this with pp instead of steam.
            }

        }
        //pretty overkill but MatchManiac is 10,000 matches and i'm only going to be checking in multiples of 10
        //for all of them, probably.
        for (int i = 1; i < steamStats.steamStatKeys.Length + 1; i++)
        {
            for (int j = 10; j < 10000; j+=10)
            {
                PlayerPrefsX.SetBool("stat_" + i + j, false);

            }
        }
        SteamUserStats.RequestCurrentStats();
    }
    public void ResetAllAchievements()
    {
        for (int i = 0; i < SteamUserStats.GetNumAchievements(); i++)
        {
            SteamUserStats.ClearAchievement(SteamUserStats.GetAchievementName((uint)i));
        }
        //SteamUserStats.ClearAchievement("MATCH_3S");
    }


    private void Awake()
    {
        //DontDestroyOnLoad(this);
        if(SM==null){
            SM = this;
        }else{
            Destroy(gameObject);
        }

    }
    // Start is called before the first frame update
    void Start()
    {
        steamStats = GetComponent<SteamStatsAndAchievements>();
        //assign accountpref strings based on UI Manager's endgame stats:
        accountPrefs = new string[UIManager.UIM.p1Texts.Length];
        for (int i = 0; i < UIManager.UIM.p1Texts.Length; i++)
        {
            accountPrefs[i] = UIManager.UIM.p1Texts[i].name;
        }
        matchesMade = new int[5, 10];//setting this here now, but may not need to if we use the SetStats function
        pStats = new int[3, 20];
        InitEndGame();

    }

    // Update is called once per frame
    void Update()
    {
        //keeping && !GameManager.GM.everythingPause out for now but this probably needs to be fixed
        //in a polish run or something.
        if (GameManager.GM.playingGame)
        {
            GameTimerUp();
        }


    }

    void SetStatsBasedOnNumOfPlayers(int players){

        minionKills = new int[players];
        matchesMade = new int[players, 10];
    }

    public void GameTimerUp(){
        gameTime += Time.deltaTime;
        UIManager.UIM.gameTime.text = FormatTime(gameTime);
    }

    public void ResetStats(){
        System.Array.Clear(minionKills,0,minionKills.Length);

        for (int i = 0; i < matchesMade.Length; i++){ //no clue what 2d array length returns.  the number of rows, apparently.
            for (int j = 0; j < 10; j++){
                matchesMade[i, j] = 0;
            }
        }
    }

    public string FormatTime(float timeToFormat) //doing this enough that i should make it a function.  call it to format a time.
    {
        int fminutes = Mathf.FloorToInt(timeToFormat / 60f);
        int fseconds = Mathf.FloorToInt(timeToFormat - fminutes * 60);
        string formattedTime = string.Format("{0:00}:{1:00}", fminutes, fseconds);

        return formattedTime;
    }


    public void IncrementAccountMatches(string pin, int color, int matchAmount)
    {
        if (SteamManager.Initialized)
        {
            //SteamUserStats.GetStat("match_" + matchAmount, out steamStats.steamMatches[matchAmount]);
            steamStats.steamMatches[matchAmount]++;
            //SteamUserStats.SetStat("match_" + matchAmount, steamStats.steamMatches[matchAmount]);
            //SteamUserStats.StoreStats();
            //int s;
            //SteamUserStats.GetStat("match_" + matchAmount, out s);
            //float p;
            //bool chiev;
            //SteamUserStats.GetAchievement("MATCH_3S", out chiev);
            //if (!chiev)
            //{
            //    Debug.Log(SteamUserStats.GetAchievement("MATCH_3S", out chiev));
            //}
  
            steamStats.m_bStoreStats = true;
            //SteamUserStats.GetAchievementAchievedPercent("MATCH_3S", out p);
            //print(p.ToString()+"%");

        }
        else
        {
            int matches = PlayerPrefs.GetInt(pin + color + "match" + matchAmount);
            matches++;
            PlayerPrefs.SetInt(pin + color + "match" + matchAmount, matches);

        }
        //print("player " + pin + " has made " + matches + " match" + matchAmount + "s.");
    }

    
    public void ClearAccountStats(string pin)
    {
        if(PlayerPrefs.HasKey(pin))
        {
            for (int i = 0; i < GameManager.GM.actualColors.Length; i++)
            {
                for(int j=3; j < 10; j++)
                {
                    PlayerPrefs.DeleteKey(pin +i+ "match" + j);

                }

            }
            print(pin + " stats deleted.");

        }

    }

    public void ShowStats()
    {
        //print("Loading stats for " + tempPin);
        turnOnList.Clear();
        HideStatTexts();
        //make new arrays for the sums:
        int[] colorSumsi = new int[colorSums.Length];
        int[] matchSumsi = new int[matchSums.Length];
        for (int i = 0; i < GameManager.GM.actualColors.Length; i++) //6 colors atm
        {
            for (int j = 3; j < 10; j++)//match9 is the most atm.
            {
                //i = the color (0 = blue)
                //j = match num (3 = match 3s)
                int stat = PlayerPrefs.GetInt(tempPin + i + "match" + j);
                //wanna do this on delay, maybe?
                //going to add them to a list, then loop through and turn them all on.
                turnOnList.Add(bigBoiFromOutkast[i].matchNumbers[j - 3]);

                bigBoiFromOutkast[i].matchNumbers[j - 3].text = stat.ToString();
                //colorsums:
                colorSumsi[i] += stat;
               
            }

            colorSums[i].text = colorSumsi[i].ToString();
            colorSums[i].color = GameManager.GM.actualColors[i];
            turnOnList.Add(colorSums[i]);
        }

        int tt = 0;

        //i'm putting this in a separate loop because my
        //brain can't deal with keeping it in the other.
        for (int i = 3; i < 10; i++)
        {

            for (int j = 0; j < GameManager.GM.actualColors.Length; j++)
            {
                int p = 0;
                int.TryParse(bigBoiFromOutkast[j].matchNumbers[i-3].text, out p);
                matchSumsi[i - 3] += p;
                tt += p;

            }
            matchSums[i - 3].text = matchSumsi[i-3].ToString();
            turnOnList.Add(matchSums[i-3]);



        }
        //misc stats on pg2:
        for(int i=0; i<statScreenStatNums.Length; i++)
        {
            statScreenStatNums[i].enabled = true; //no anim on page 2 atm.

            statScreenStatNums[i].text = PlayerPrefs.GetInt(tempPin + accountPrefs[i]).ToString();
        }
        totalTotalTxt.text = tt.ToString();
        turnOnList.Add(totalTotalTxt);
        StopAllCoroutines(); //otherwise it was starting in the middle of the anim sometimes.  not a huge deal.
        StartCoroutine(EnableTextsDelay(0.05f));
        

    }


    public void HideStatTexts()
    {
        totalTotalTxt.enabled = false;
        for (int i = 0; i < GameManager.GM.actualColors.Length; i++) //6 colors atm
        {
            colorSums[i].enabled = false;
            for (int j = 3; j < 10; j++)//match9 is the most atm.
            {
                bigBoiFromOutkast[i].matchNumbers[j - 3].enabled = false;
                if(matchSums[j-3].enabled)
                {
                    matchSums[j - 3].enabled = false;
                }
            }

        }
        for (int i = 0; i < statScreenStatNums.Length; i++)
        {
            statScreenStatNums[i].enabled = false;
        }
    }
    public IEnumerator EnableTextsDelay(float del)
    {
        for (int i = 0; i < turnOnList.Count; i++)
        {
            turnOnList[i].enabled = true;
            yield return new WaitForSeconds(del);
        }
    }

    void InitEndGame()
    {
        for (int i = 0; i < 1; i++)
        {
            for(int j=0; j< 10; j++)
            {
                pStats[i,j] = 0;

            }

        }
    }

    //i'm keeping total matches separate from minions spawned cause if you
    //double spawn a minion, that shouldn't count as two matches.

    //probably a nicer way to do this but i'm going with what works
    /// <summary>
    /// 0=mSpawned; 1=mKills; 2=researchLvl; 3=itemsPurchased; 4=peons; 5=bolts; 6=gemsDest; 7=swaps; 8=matchesTotal
    /// </summary>
    /// <param name="pNum"></param>
    /// <param name="statKey"></param>
    /// Steam stats will be +1 index of statKey (ex minionsSpawned = 1, not 0)
    public void IncrementStat(int pNum, int statKey)
    {
        pStats[pNum, statKey]++;
        //!!Only storing stats for player 1.
        if(SteamManager.Initialized && pNum==0){
            //SteamUserStats.GetStat("stat_" + statKey+1, out steamStats.steamStatKeys[statKey+1]);
            steamStats.steamStatKeys[statKey]++;
            //SteamUserStats.SetStat("stat_" + statKey+1, steamStats.steamStatKeys[statKey]);
            //SteamUserStats.StoreStats();
            //int s;
            //SteamUserStats.GetStat("match_" + matchAmount, out s);
            //float p;
            //bool chiev;
            //SteamUserStats.GetAchievement("MATCH_3S", out chiev);
            //if (!chiev)
            //{
            //    Debug.Log(SteamUserStats.GetAchievement("MATCH_3S", out chiev));
            //}
         
            steamStats.m_bStoreStats = true;
            //switch (statKey)
            //{
            //    case 8:
            //        //print("popup");
            //        float f = -777;
            //        //float j = -888;
            //        SteamUserStats.GetStat("stat_9", out f);
            //        //SteamUserStats.GetStat("stat_8", out j);

            //        print("stat_9 is " + f + " before popup");
            //        //print("stat_8 is " + j + " before popup");

            //        SteamUserStats.IndicateAchievementProgress("MATCH_MASTER", (uint)f, 10000);

            //        break;
            //}
            //SteamUserStats.GetAchievementAchievedPercent("MATCH_3S", out p);
            //print(p.ToString()+"%");
        }
    }


    public void IncrementWins(int playerNum)
    {
        wins[playerNum]++;
        if (PlayerManager.PM.players.Count > 1)
        {
            wins_p[playerNum]++;
        }
        else
        {
            wins_ai[playerNum]++;
        }
        //for now, this is only logging for player 1.
        if (SteamManager.Initialized  && playerNum==0)
        {
            //!!Might also have to get second player steam info if that's something I can do.
            //SteamUserStats.GetStat("wins", out steamStats.wins);
            //SteamUserStats.GetStat("wins_p", out steamStats.wins_p);
            //SteamUserStats.GetStat("wins_ai", out steamStats.wins_ai);

            steamStats.wins++;
            if (PlayerManager.PM.players.Count > 1)
            {
                steamStats.wins_p++;
            }
            else
            {
                steamStats.wins_ai++;
            }

            //SteamUserStats.SetStat("wins", steamStats.wins);
            //SteamUserStats.SetStat("wins_p", steamStats.wins_p);
            //SteamUserStats.SetStat("wins_ai", steamStats.wins_ai);
            steamStats.m_bStoreStats = true;
        }
    }

    public void IncrementLosses(int playerNum)
    {
        losses[playerNum]++;

        if (PlayerManager.PM.players.Count > 1)
        {
            losses_p[playerNum]++;
        }
        else
        {
            losses_ai[playerNum]++;
        }

        //for now, only logging for player 1:
        if (SteamManager.Initialized && playerNum ==0)
        {
            //SteamUserStats.GetStat("losses", out steamStats.losses);
            //SteamUserStats.GetStat("losses_p", out steamStats.losses_p);
            //SteamUserStats.GetStat("losses_ai", out steamStats.losses_ai);

            steamStats.losses++;
            if (PlayerManager.PM.players.Count > 1)
            {
                steamStats.losses_p++;
            }
            else
            {
                steamStats.losses_ai++;
            }

            //SteamUserStats.SetStat("losses", steamStats.losses);
            //SteamUserStats.SetStat("losses_p", steamStats.losses_p);
            //SteamUserStats.SetStat("losses_ai", steamStats.losses_ai);
            steamStats.m_bStoreStats = true;

        }
    }

        //atm, stats are added to an account at the END of a game
        public void AddRoundStatsToAccount(string pin, int pNum)
    {
        for(int i =0; i<accountPrefs.Length; i++)
        {
            int temp = PlayerPrefs.GetInt(pin + accountPrefs[i]);
            //print("player " + pin + " accountPrefs[i] was " + temp);

            temp += pStats[pNum, i];
            PlayerPrefs.SetInt(pin + accountPrefs[i], temp);
            //print("player " + pin + " accountPrefs[i] now = " + temp);
        }
    }


}
