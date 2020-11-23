using UnityEngine;
using System.Collections;
using System.ComponentModel;
using Steamworks;

// This is a port of StatsAndAchievements.cpp from SpaceWar, the official Steamworks Example.
public class SteamStatsAndAchievements : MonoBehaviour
{
    private enum Achievement : int
    {
        MATCH_3S,
        MATCH_4S,
        MATCH_5S,
        MATCH_6S,
        MATCH_7S,
        MATCH_8S,
        MATCH_9S,

        WIN_BRONZE,
        WIN_SILVER,
        WIN_GOLD,
        WIN_ONE_AI,
        WIN_GOLD_AI,
        LOSE_ONE_AI,

        MATCH_MANIAC,
        MATCH_MASTER,

        ITEM_BRONZE,
        ITEM_SILVER,

        RESEARCH_BRONZE,
        RESEARCH_SILVER,

        SNIPE_BRONZE,
        SNIPE_SILVER,

        MINER_BRONZE,
        MINER_SILVER,

        PEONS_BRONZE,
        PEONS_SILVER,


        //ACH_WIN_100_GAMES,
        //ACH_HEAVY_FIRE,
        //ACH_TRAVEL_FAR_ACCUM,
        //ACH_TRAVEL_FAR_SINGLE,
    };

    private Achievement_t[] m_Achievements = new Achievement_t[] {
        new Achievement_t(Achievement.MATCH_3S, "Three's Company", ""),
                new Achievement_t(Achievement.MATCH_4S, "Fourplay", ""),
                new Achievement_t(Achievement.MATCH_5S, "Cinco de Matcho", ""),
                new Achievement_t(Achievement.MATCH_6S, "Hex Flex", ""),
                new Achievement_t(Achievement.MATCH_7S, "Lucky!", ""),
                new Achievement_t(Achievement.MATCH_8S, "Greight Matching!", ""),
                new Achievement_t(Achievement.MATCH_9S, "Cloud Nine", ""),

        new Achievement_t(Achievement.WIN_BRONZE, "Getting the hang of it", ""),
        new Achievement_t(Achievement.WIN_SILVER, "Good game, well played", ""),
        new Achievement_t(Achievement.WIN_GOLD, "Chicken dinner", ""),
        new Achievement_t(Achievement.WIN_ONE_AI, "Pod bay doors: opened", ""),
        new Achievement_t(Achievement.WIN_GOLD_AI, "1v1 Comp stomp", ""),
        new Achievement_t(Achievement.LOSE_ONE_AI, "I'm sorry, Dave...", ""),

        new Achievement_t(Achievement.MATCH_MANIAC, "Match Match Maniac", ""),
        new Achievement_t(Achievement.MATCH_MASTER, "Match Match Master", ""),

        new Achievement_t(Achievement.ITEM_BRONZE, "Shopping Bags", ""),
        new Achievement_t(Achievement.ITEM_SILVER, "Poppin Tags", ""),
        new Achievement_t(Achievement.RESEARCH_BRONZE, "Remember Me!", ""),
        new Achievement_t(Achievement.RESEARCH_SILVER, "Genius", ""),
        new Achievement_t(Achievement.SNIPE_BRONZE, "There are Many Like It...", ""),
        new Achievement_t(Achievement.SNIPE_SILVER, "Sniper Elite", ""),
        new Achievement_t(Achievement.MINER_BRONZE, "Bolt Brain", ""),
        new Achievement_t(Achievement.MINER_SILVER, "Fat Pockets", ""),
        new Achievement_t(Achievement.PEONS_BRONZE, "Zug, zug", ""),
        new Achievement_t(Achievement.PEONS_SILVER, "Macro Manager", "")



        //new Achievement_t(Achievement.ACH_WIN_100_GAMES, "Champion", ""),
        //new Achievement_t(Achievement.ACH_TRAVEL_FAR_ACCUM, "Interstellar", ""),
        //new Achievement_t(Achievement.ACH_TRAVEL_FAR_SINGLE, "Orbiter", "")
    };

    // Our GameID
    private CGameID m_GameID;

    // Did we get the stats from Steam?
    private bool m_bRequestedStats;
    private bool m_bStatsValid;

    // Should we store stats this frame?
    public bool m_bStoreStats;

    // Current Stat details
    //private float m_flGameFeetTraveled;
    //private float m_ulTickCountGameStart;
    //private double m_flGameDurationSeconds;

    // Persisted Stat details
    //these are duplicated from StatsManager but i want to keep steam
    //stat tracking separate
    private int match_3s;
    public int[] steamMatches = new int[10];
    public int[] steamStatKeys = new int[10];
    public int wins, wins_p, wins_ai, losses, losses_p, losses_ai;

    //private int m_nTotalGamesPlayed;
    //private int m_nTotalNumWins;
    //private int m_nTotalNumLosses;
    //private float m_flTotalFeetTraveled;
    //private float m_flMaxFeetTraveled;
    //private float m_flAverageSpeed;


    protected Callback<UserStatsReceived_t> m_UserStatsReceived;
    protected Callback<UserStatsStored_t> m_UserStatsStored;
    protected Callback<UserAchievementStored_t> m_UserAchievementStored;

    void OnEnable()
    {
        if (!SteamManager.Initialized)
            return;

        // Cache the GameID for use in the Callbacks
        m_GameID = new CGameID(SteamUtils.GetAppID());

        m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
        m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

        // These need to be reset to get the stats upon an Assembly reload in the Editor.
        m_bRequestedStats = false;
        m_bStatsValid = false;
    }

    private void Update()
    {
        if (!SteamManager.Initialized)
            return;

        if (!m_bRequestedStats)
        {
            // Is Steam Loaded? if no, can't get stats, done
            if (!SteamManager.Initialized)
            {
                m_bRequestedStats = true;
                return;
            }

            // If yes, request our stats
            bool bSuccess = SteamUserStats.RequestCurrentStats();

            // This function should only return false if we weren't logged in, and we already checked that.
            // But handle it being false again anyway, just ask again later.
            m_bRequestedStats = bSuccess;
        }

        if (!m_bStatsValid)
            return;

        // Get info from sources

        // Evaluate achievements
        foreach (Achievement_t achievement in m_Achievements)
        {
            if (achievement.m_bAchieved)
                continue;

            switch (achievement.m_eAchievementID)
            {
                case Achievement.MATCH_3S:
                    
                    if (steamMatches[3] >= 100)
                    {

                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_4S:
                    if (steamMatches[4] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_5S:
                    if (steamMatches[5] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_6S:
                    if (steamMatches[6] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_7S:
                    if (steamMatches[7] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_8S:
                    if (steamMatches[8] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_9S:
                    if (steamMatches[9] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.WIN_BRONZE:
                    if (wins >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.WIN_SILVER:
                    if (wins >= 50)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.WIN_GOLD:
                    if (wins >= 200)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.WIN_ONE_AI:
                    if (wins_ai >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.WIN_GOLD_AI:
                    if (wins_ai >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.LOSE_ONE_AI:
                    if (losses_ai >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_MANIAC:
                    
                    if (steamStatKeys[9] >= 1000)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MATCH_MASTER:
                    if (steamStatKeys[9] >= 10000)
                    {
                       UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.ITEM_BRONZE:
                    if (steamStatKeys[3] >= 50)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.ITEM_SILVER:
                    if (steamStatKeys[3] >= 100)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.RESEARCH_BRONZE:
                    if (steamStatKeys[2] >= 40)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.RESEARCH_SILVER:
                    if (steamStatKeys[2] >= 400)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.SNIPE_BRONZE:
                    if (steamStatKeys[1] >= 30)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.SNIPE_SILVER:
                    if (steamStatKeys[1] >= 300)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MINER_BRONZE:
                    if (steamStatKeys[5] >= 300)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.MINER_SILVER:
                    if (steamStatKeys[5] >= 5000)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.PEONS_BRONZE:
                    if (steamStatKeys[4] >= 50)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.PEONS_SILVER:
                    if (steamStatKeys[4] >= 500)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
            }
        }

        //Store stats in the Steam database if necessary
        if (m_bStoreStats)
        {
            // already set any achievements in UnlockAchievement

            // set stats
            //set matches:
            for (int i = 3; i < steamMatches.Length; i++)
            {
                SteamUserStats.SetStat("match_" + i, steamMatches[i]);
            }
            //set other stats:
            for (int i = 1; i < steamStatKeys.Length; i++)
            {
                SteamUserStats.SetStat("stat_" + i, steamStatKeys[i-1]); //ex, statkey 8 (matches) -> stat_9 on steam backend
            }
            SteamUserStats.SetStat("wins", wins);
            SteamUserStats.SetStat("wins_p", wins_p);
            SteamUserStats.SetStat("wins_ai", wins_ai);
            SteamUserStats.SetStat("losses", losses);
            SteamUserStats.SetStat("losses_p", losses_p);
            SteamUserStats.SetStat("losses_ai", losses_ai);

            PopupLogic();




            //SteamUserStats.SetStat("Match3s", steamMatches[3]);

            //SteamUserStats.SetStat("NumGames", m_nTotalGamesPlayed);
            //SteamUserStats.SetStat("NumWins", m_nTotalNumWins);
            //SteamUserStats.SetStat("NumLosses", m_nTotalNumLosses);
            //SteamUserStats.SetStat("FeetTraveled", m_flTotalFeetTraveled);
            //SteamUserStats.SetStat("MaxFeetTraveled", m_flMaxFeetTraveled);
            //// Update average feet / second stat
            //SteamUserStats.UpdateAvgRateStat("AverageSpeed", m_flGameFeetTraveled, m_flGameDurationSeconds);
            //// The averaged result is calculated for us
            //SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);

            bool bSuccess = SteamUserStats.StoreStats();
            // If this failed, we never sent anything to the server, try
            // again later.
            m_bStoreStats = !bSuccess;
        }
    }

    public void PopupLogic()
    {
        /// 0=mSpawned; 1=mKills; 2=researchLvl; 3=itemsPurchased; 
        /// 4=peons; 5=bolts; 6=gemsDest; 7=swaps; 8=matchesTotal

        //STATS:
        //int[] stats = new int[13];
        for (int i = 0; i < steamStatKeys.Length; i++)
        {
            //SteamUserStats.GetStat("stat_"+i+1, out stats[i]); //ex, statkey 8 (matches) -> stat_9 on steam backend
            if (steamStatKeys[i] != 0 && !PlayerPrefsX.GetBool("stat_" + (i) + steamStatKeys[i]))
            {
                SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);

                switch (i)
                {
                    //the first number in the if is the earlier achievement
                    //so the progress for the big chievo doesn't
                    //show when you get the smaller one.
                    case 8:
                        if (steamStatKeys[i] !=1000 && steamStatKeys[i] % 10 == 0)
                        {
                            SteamUserStats.IndicateAchievementProgress("MATCH_MASTER", (uint)steamStatKeys[i], 10000);
                            PlayerPrefsX.SetBool("stat_" + i + steamStatKeys[i], true);

                        }

                        break;
                    case 5:
                        if (steamStatKeys[i] !=300 && steamStatKeys[i] % 50 == 0)
                        {
                            SteamUserStats.IndicateAchievementProgress("MINER_SILVER", (uint)steamStatKeys[i], 5000);
                            PlayerPrefsX.SetBool("stat_" + i + steamStatKeys[i], true);

                        }
                        break;
                    case 1:
                        if (steamStatKeys[i] != 30 && steamStatKeys[i] % 30 == 0)
                        {
                            SteamUserStats.IndicateAchievementProgress("SNIPE_SILVER", (uint)steamStatKeys[i], 300);
                            PlayerPrefsX.SetBool("stat_" + i + steamStatKeys[i], true);

                        }
                        break;

                    case 3:
                        if (steamStatKeys[i] !=50 && steamStatKeys[i] % 10 == 0)
                        {
                            SteamUserStats.IndicateAchievementProgress("ITEM_SILVER", (uint)steamStatKeys[i], 100);
                            PlayerPrefsX.SetBool("stat_" + i + steamStatKeys[i], true);

                        }
                        break;
                    case 4:
                        if (steamStatKeys[i] !=50 && steamStatKeys[i] % 10 == 0)
                        {
                            SteamUserStats.IndicateAchievementProgress("PEONS_SILVER", (uint)steamStatKeys[i], 500);
                            PlayerPrefsX.SetBool("stat_" + i + steamStatKeys[i], true);

                        }
                        break;

                }
            }

        }

        //MATCHES:
        int[] matches = new int[10];
        for (int i = 3; i < steamMatches.Length; i++)
        {
            SteamUserStats.GetStat("match_" + i, out matches[i]);
            //only show it if the playerprefs hasn't been flagged
            //this is to stop duplicate popups
            //ex. if match3 = 10, it pops up, then match 4 = 10, match 3 shouldn't pop up again, even if it's still at 10
            if (matches[i] != 0 && !PlayerPrefsX.GetBool("match_"+i+matches[i]) && matches[i] % 10 == 0)
            {
                SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);
                SteamUserStats.IndicateAchievementProgress("MATCH_"+i+"S", (uint)matches[i], 100);
                PlayerPrefsX.SetBool("match_" + i+matches[i], true);
            }
            //switch (i)
            //{
            //    case 8:
            //        if (stats[i] != 0 && stats[i] % 10 == 0)
            //        {
            //            SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);
            //            SteamUserStats.IndicateAchievementProgress("MATCH_MASTER", (uint)stats[i], 10000);
            //        }

            //        break;
            //    case 5:
            //        if (stats[i] != 0 && stats[i] % 50 == 0)
            //        {
            //            SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);
            //            SteamUserStats.IndicateAchievementProgress("MINER_SILVER", (uint)stats[i], 5000);
            //        }
            //        break;

            //}


        }



    }

    //-----------------------------------------------------------------------------
    // Purpose: Accumulate distance traveled
    //-----------------------------------------------------------------------------
    //public void AddDistanceTraveled(float flDistance)
    //{
    //    m_flGameFeetTraveled += flDistance;
    //}

    //-----------------------------------------------------------------------------
    // Purpose: Game state has changed
    //-----------------------------------------------------------------------------
    //public void OnGameStateChange(EClientGameState eNewState)
    //{
    //    if (!m_bStatsValid)
    //        return;

    //    if (eNewState == EClientGameState.k_EClientGameActive)
    //    {
    //        // Reset per-game stats
    //        m_flGameFeetTraveled = 0;
    //        m_ulTickCountGameStart = Time.time;
    //    }
    //    else if (eNewState == EClientGameState.k_EClientGameWinner || eNewState == EClientGameState.k_EClientGameLoser)
    //    {
    //        if (eNewState == EClientGameState.k_EClientGameWinner)
    //        {
    //            m_nTotalNumWins++;
    //        }
    //        else
    //        {
    //            m_nTotalNumLosses++;
    //        }

    //        // Tally games
    //        m_nTotalGamesPlayed++;

    //        // Accumulate distances
    //        m_flTotalFeetTraveled += m_flGameFeetTraveled;

    //        // New max?
    //        if (m_flGameFeetTraveled > m_flMaxFeetTraveled)
    //            m_flMaxFeetTraveled = m_flGameFeetTraveled;

    //        // Calc game duration
    //        m_flGameDurationSeconds = Time.time - m_ulTickCountGameStart;

    //        // We want to update stats the next frame.
    //        m_bStoreStats = true;
    //    }
    //}

    //-----------------------------------------------------------------------------
    // Purpose: Unlock this achievement
    //-----------------------------------------------------------------------------
    private void UnlockAchievement(Achievement_t achievement)
    {
        achievement.m_bAchieved = true;

        // the icon may change once it's unlocked
        //achievement.m_iIconImage = 0;

        // mark it down
        SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());

        // Store stats end of frame
        m_bStoreStats = true;
    }

    //-----------------------------------------------------------------------------
    // Purpose: We have stats data from Steam. It is authoritative, so update
    //			our data with those results now.
    //-----------------------------------------------------------------------------
    private void OnUserStatsReceived(UserStatsReceived_t pCallback)
    {
        if (!SteamManager.Initialized)
            return;

        // we may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (EResult.k_EResultOK == pCallback.m_eResult)
            {
                Debug.Log("Received stats and achievements from Steam\n");

                m_bStatsValid = true;

                // load achievements
                foreach (Achievement_t ach in m_Achievements)
                {
                    bool ret = SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
                    if (ret)
                    {
                        ach.m_strName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
                        ach.m_strDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
                    }
                    else
                    {
                        Debug.LogWarning("SteamUserStats.GetAchievement failed for Achievement " + ach.m_eAchievementID + "\nIs it registered in the Steam Partner site?");
                    }
                }



                // load stats
                for (int i = 3; i < steamMatches.Length; i++)
                {
                    SteamUserStats.GetStat("match_" + i, out steamMatches[i]);
                }
                //set other stats:
                for (int i = 1; i < steamStatKeys.Length+1; i++)
                {
                    SteamUserStats.GetStat("stat_" + i, out steamStatKeys[i-1]);
                }
                SteamUserStats.GetStat("wins", out wins);
                SteamUserStats.GetStat("wins_p", out wins_p);
                SteamUserStats.GetStat("wins_ai", out wins_ai);
                SteamUserStats.GetStat("losses", out losses);
                SteamUserStats.GetStat("losses_p", out losses_p);
                SteamUserStats.GetStat("losses_ai", out losses_ai);
            }
            else
            {
                Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
            }
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: Our stats data was stored!
    //-----------------------------------------------------------------------------
    private void OnUserStatsStored(UserStatsStored_t pCallback)
    {
        // we may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (EResult.k_EResultOK == pCallback.m_eResult)
            {
                Debug.Log("StoreStats - success");
            }
            else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
            {
                // One or more stats we set broke a constraint. They've been reverted,
                // and we should re-iterate the values now to keep in sync.
                Debug.Log("StoreStats - some failed to validate");
                // Fake up a callback here so that we re-load the values.
                UserStatsReceived_t callback = new UserStatsReceived_t();
                callback.m_eResult = EResult.k_EResultOK;
                callback.m_nGameID = (ulong)m_GameID;
                OnUserStatsReceived(callback);
            }
            else
            {
                Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
            }
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: An achievement was stored
    //-----------------------------------------------------------------------------
    private void OnAchievementStored(UserAchievementStored_t pCallback)
    {
        // We may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (0 == pCallback.m_nMaxProgress)
            {
                Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
            }
            else
            {
                Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
            }
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: Display the user's stats and achievements
    //-----------------------------------------------------------------------------
    public void Render()
    {
        //if (!SteamManager.Initialized)
        //{
        //    GUILayout.Label("Steamworks not Initialized");
        //    return;
        //}

        //GUILayout.Label("m_ulTickCountGameStart: " + m_ulTickCountGameStart);
        //GUILayout.Label("m_flGameDurationSeconds: " + m_flGameDurationSeconds);
        //GUILayout.Label("m_flGameFeetTraveled: " + m_flGameFeetTraveled);
        //GUILayout.Space(10);
        //GUILayout.Label("NumGames: " + m_nTotalGamesPlayed);
        //GUILayout.Label("NumWins: " + m_nTotalNumWins);
        //GUILayout.Label("NumLosses: " + m_nTotalNumLosses);
        //GUILayout.Label("FeetTraveled: " + m_flTotalFeetTraveled);
        //GUILayout.Label("MaxFeetTraveled: " + m_flMaxFeetTraveled);
        //GUILayout.Label("AverageSpeed: " + m_flAverageSpeed);

        //GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, 800));
        //foreach (Achievement_t ach in m_Achievements)
        //{
        //    GUILayout.Label(ach.m_eAchievementID.ToString());
        //    GUILayout.Label(ach.m_strName + " - " + ach.m_strDescription);
        //    GUILayout.Label("Achieved: " + ach.m_bAchieved);
        //    GUILayout.Space(20);
        //}

        //// FOR TESTING PURPOSES ONLY!
        //if (GUILayout.Button("RESET STATS AND ACHIEVEMENTS"))
        //{
        //    SteamUserStats.ResetAllStats(true);
        //    SteamUserStats.RequestCurrentStats();
        //    //OnGameStateChange(EClientGameState.k_EClientGameActive);
        //}
        //GUILayout.EndArea();
    }
    public void Increment()
    {

    }

    private class Achievement_t
    {
        public Achievement m_eAchievementID;
        public string m_strName;
        public string m_strDescription;
        public bool m_bAchieved;

        /// <summary>
        /// Creates an Achievement. You must also mirror the data provided here in https://partner.steamgames.com/apps/achievements/yourappid
        /// </summary>
        /// <param name="achievementID">The "API Name Progress Stat" used to uniquely identify the achievement.</param>
        /// <param name="name">The "Display Name" that will be shown to players in game and on the Steam Community.</param>
        /// <param name="desc">The "Description" that will be shown to players in game and on the Steam Community.</param>
        public Achievement_t(Achievement achievementID, string name, string desc)
        {
            m_eAchievementID = achievementID;
            m_strName = name;
            m_strDescription = desc;
            m_bAchieved = false;
        }
    }
}