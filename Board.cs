using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

//from tut: https://www.youtube.com/watch?v=vAfZ5gFaThI&list=PL4vbr3u7UKWrxEz75MqmTDd899cYAvQ_B&index=3

    public enum GameState
{
    wait,
    move
}
public class Board : MonoBehaviour
{
    public AnimationCurve randomCurve;
    public GameState currentState = GameState.move;
    public bool autoPlay;//if on, the board will randomly delete gems after delay
    public int difficulty = 0;//0=easy, 1=med, so on.
    int autoResearchIndex, autoMatchIndex; //this will count up and serve as a delay for hard/super hard modes.

    public bool doneAutoDest;
    public bool challengeOverride;
    public int numToAutoKill = 2;
    public int width, height;
    public float gemOffset;
    public float xSpacer, ySpacer;
    public GameObject tilePrefab;
    public GameObject dotToSpawn;
    public Gem[,] allTiles;
    public GameObject[,] allDots;

    public FindMatches matchMaker;
    public PlayerShoot myPlayer;
    public int teamNum;
    public Attractor teamVac;

    public List<GameObject> myPaths; //this will hold Top, Mid, Bottom, Jungle paths in that order.
    public List<GameObject> monumentLevels; //these are the stages of the monument that get turned on, etc.
                                            //public int researchLevel; //a simpler num - how many levels have been researched so far?
    public bool maxReasearch;
    public GameObject researchMaxColl;
    public GameObject waitWall;
    //myvars:
    private int reserveToSpawn;
    public int comboNumber, cascadesNum, totalGemsDestroyed;
    public GameObject[] lanes; //this is going to be called by PlayerShoot to turn on particles.

    public MeshRenderer boundGrid;

    [Header("Delay Vars")]
    //these are for the many coroutine delays we have.
    //now we can tweak them and maybe change them in real-time from buffs, etc.
    public float backToPlayDelay;
    public float checkDelay, refillDelay, destroyDelay;
    public float refillDelayAI; //for when the board is playing itself.
    float gemDieAnimTime;

    [Header("Stats")]
    public int[] gemsDest;
    public bool[] gFSatisfied;

    public GameObject gfParts;

    // Start is called before the first frame update
    void Start()
    {
        allTiles = new Gem[width, height];
        allDots = new GameObject[width, height];
        gemsDest = new int[GameManager.GM.actualColors.Length];
        gFSatisfied = new bool[GameManager.GM.actualColors.Length];
        matchMaker = GetComponent<FindMatches>();
        if(challengeOverride)
        {
            ChallengeSetup();
        }else{
            Setup();
        }
        if (autoPlay){
            InvokeRepeating("DestroyAuto", 1f,1f);
        }

    }


    private void ChallengeSetup()
    {
        int k = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 tempPos = new Vector2(i * xSpacer, j * ySpacer);
                //GameObject g = Instantiate(tilePrefab, tempPos, Quaternion.identity);
                //g.transform.parent = this.transform; //set parent to be board;
                //g.name = "( " + i + ", " + j + " )";

                //also make the actual dots:
                //GameObject dot = Instantiate(dotToSpawn, tempPos, Quaternion.identity, this.transform);
                GameObject dot = Instantiate(dotToSpawn, transform, false);
                dot.transform.localPosition = new Vector2(tempPos.x, tempPos.y + gemOffset);
                Gem thisGem = dot.GetComponent<Gem>();
                //print(GetComponentInParent<Board>().NextNumber());
                thisGem.targetPos = tempPos;
                thisGem.brd = this;

                thisGem.matchMaker = GetComponent<FindMatches>();
                thisGem.SetCoordinates(i, j);
             
                thisGem.SetColor(GameManager.GM.challengeBoard[k]);

                //int maxIterations = 0;
                //if (GameManager.GM.noMatchesInitial)
                //{
                //    //not sure if we really want a while incrementing the NextNumber()
                //    while (MatchesAt(i, j, thisGem) && maxIterations < 100)
                //    {
                //        thisGem.SetColor(GameManager.GM.colorsToSpawn[NextNumber()]);
                //        maxIterations++;
                //    }
                //    maxIterations = 0;
                //}
                thisGem.CheckMyBuffHints();

                //dot.transform.parent = this.transform;
                dot.name = "( " + i + ", " + j + " )";
                allDots[i, j] = dot;

                k++;
            }
        }
        GameManager.GM.ChallengeIndex(0);

    }
    public void ChallengeUpdate()
    {
        int k = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                allDots[i, j].GetComponent<Gem>().SetColor(GameManager.GM.challengeBoard[k]);
                k++;
            }
        }
    }

    private void Setup()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 tempPos = new Vector2(i * xSpacer, j * ySpacer);
                //GameObject g = Instantiate(tilePrefab, tempPos, Quaternion.identity);
                //g.transform.parent = this.transform; //set parent to be board;
                //g.name = "( " + i + ", " + j + " )";

                //also make the actual dots:
                //GameObject dot = Instantiate(dotToSpawn, tempPos, Quaternion.identity, this.transform);
                GameObject dot = Instantiate(dotToSpawn, transform, false);
                dot.transform.localPosition = new Vector2(tempPos.x, tempPos.y + gemOffset);
                Gem thisGem = dot.GetComponent<Gem>();
                //print(GetComponentInParent<Board>().NextNumber());
                thisGem.targetPos = tempPos;
                thisGem.brd = this;

                thisGem.matchMaker = GetComponent<FindMatches>();
                thisGem.SetCoordinates(i, j);
                thisGem.SetColor(GameManager.GM.colorsToSpawn[NextNumber()]);

                int maxIterations = 0;
                if (GameManager.GM.noMatchesInitial)
                {
                    //not sure if we really want a while incrementing the NextNumber()
                    while (MatchesAt(i, j, thisGem) && maxIterations < 100)
                    {
                        thisGem.SetColor(GameManager.GM.colorsToSpawn[NextNumber()]);
                        maxIterations++;
                    }
                    maxIterations = 0;
                }
                thisGem.CheckMyBuffHints();

                //dot.transform.parent = this.transform;
                dot.name = "( " + i + ", " + j + " )";
                allDots[i, j] = dot;
            }
        }
    }

    //public void MakeGemsImmune()
    //{
    //    for (int i = 0; i < width; i++)
    //    {
    //        for (int j = 0; j < height; j++)
    //        {
    //            if (allDots[i, j] != null)
    //            {
    //                allDots[i, j].GetComponent<Gem>().canTakeDamage = false;
    //            }
    //        }
    //    }
    //}

    //check if there are matches in the initial board.
    //!should probably change allDots[] to be type Gem and not GameObject... eventually.
    private bool MatchesAt(int col, int row, Gem obj)
    {
        if (col > 1 && row > 1)
        {
            //check the two to the left:
            if (allDots[col - 1, row].GetComponent<Gem>().myColor == obj.myColor && allDots[col - 2, row].GetComponent<Gem>().myColor == obj.myColor)
            {
                return true;
            }
            //check the two down:
            if (allDots[col, row - 1].GetComponent<Gem>().myColor == obj.myColor && allDots[col, row - 2].GetComponent<Gem>().myColor == obj.myColor)
            {
                return true;
            }
        }
        else if (col <= 1 || row <= 1) //edge cases
        {
            if (row > 1)
            {
                if (allDots[col, row - 1].GetComponent<Gem>().myColor == obj.myColor && allDots[col, row - 2].GetComponent<Gem>().myColor == obj.myColor)
                {
                    return true;
                }
            }
            if (col > 1)
            {
                if (allDots[col - 1, row].GetComponent<Gem>().myColor == obj.myColor && allDots[col - 2, row].GetComponent<Gem>().myColor == obj.myColor)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void SwapGems(Gem first, Gem second)
    {
        int tx = first.column;
        int ty = first.row;
        Vector2 temp1 = first.transform.localPosition;
        //update the 2DA:
        allDots[second.column, second.row] = first.gameObject;
        allDots[tx, ty] = second.gameObject;
        //update the gems' positions:
        first.SetCoordinates(second.column, second.row);
        second.SetCoordinates(tx, ty);

        //actually move them:
        first.MoveYourself(second.transform.localPosition);
        second.MoveYourself(temp1);
        matchMaker.FindAllMatches();
        //!swap gem issue here?:
        //my hyp is that DestroyMatches is being called before the FindAllMatches coroutine is done
        //DestroyMatches();
    }

    public int NextNumber()
    {
        if (reserveToSpawn < GameManager.GM.colorsToSpawn.Length - 1)
        {
            reserveToSpawn++;
        }
        else
        {
            reserveToSpawn = 0;
        }
        return reserveToSpawn;
    }

    private void DestroyMatchesAt(int col, int row)
    {
        if (allDots[col, row].GetComponent<Gem>().isMatched)
        {
            //print("destroy matches at");
            gemDieAnimTime = GameManager.GM.GetClipTime(allDots[col, row].GetComponent<Gem>().anim, "GemDestroy");

            //take it out of the list of matched gems:
            matchMaker.currentMatches.Remove(allDots[col, row].GetComponent<Gem>());
            //should call the gem's destroy me function to do a bunch of cool stuff (particles).
            allDots[col, row].GetComponent<Gem>().DestroyMe();
            //Destroy(allDots[col, row]);
            allDots[col, row] = null;
        }
    }


    //this one will check whole board:
    public void DestroyMatches()
    {
        //print("Destroy matches");
        currentState = GameState.wait;
        waitWall.SetActive(true);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    DestroyMatchesAt(i, j);
                }
            }
        }
        StartCoroutine(DecreaseRowCo());
    }

    //move gems down based on how many are missing
    private IEnumerator DecreaseRowCo()
    {
        //print("decreaseRowCo");

        yield return new WaitForSeconds(gemDieAnimTime);
        int nullCount = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    nullCount++;
                }
                else if (nullCount > 0)
                {
                    //get a temp ver of it right quick:
                    Gem t = allDots[i, j].GetComponent<Gem>();
                    //actually move the gem down:
                    allDots[i, j].GetComponent<Gem>().row -= nullCount;
                    //print(nullCount);

                    //update 2DA:
                    allDots[i, j - nullCount] = allDots[i, j];


                    allDots[i, j].GetComponent<Gem>().MoveYourself(new Vector2(t.column * xSpacer, t.row * ySpacer));
                    //nullify the previous 2da pos?
                    allDots[i, j] = null;

                }
            }
            //reset this when you move to the next column:
            nullCount = 0;
        }
        yield return new WaitForSeconds(refillDelay * 0.5f);
        //now fill the board up:
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    Vector2 tempPos = new Vector2(i * xSpacer, j * ySpacer);



                    GameObject g = Instantiate(dotToSpawn, this.transform, false);
                    Gem thisGem = g.GetComponent<Gem>();
                    thisGem.SetColor(GameManager.GM.colorsToSpawn[NextNumber()]);

                    int maxIterations = 0;
                    while (MatchesAt(i, j, thisGem) && maxIterations < 100)
                    {
                        thisGem.SetColor(GameManager.GM.colorsToSpawn[NextNumber()]);
                        maxIterations++;
                    }
                    maxIterations = 0;
                    g.transform.localPosition = new Vector2(tempPos.x, tempPos.y + gemOffset);
                    g.GetComponent<Gem>().targetPos = tempPos;
                    g.GetComponent<Gem>().brd = this;
                    g.GetComponent<Gem>().matchMaker = GetComponent<FindMatches>();
                    g.GetComponent<Gem>().SetCoordinates(i, j);
                    thisGem.CheckMyBuffHints();

                    allDots[i, j] = g;
                }
            }
        }
        //!swap gem issue here?:
        matchMaker.FindAllMatches();
    }

    private bool MatchesOnBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    //if any of them is matched, return true:
                    if (allDots[i, j].GetComponent<Gem>().isMatched)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    //!I think this is getting stuck after swapping gems
    //but only after i added the anim???
    private IEnumerator FillBoardCo()
    {
        RefillBoard();

        yield return new WaitForSeconds(refillDelay);
        //if there are still matches on the board:
        while (MatchesOnBoard())
        {
            cascadesNum++;
            DestroyMatches();
            yield return new WaitForSeconds(refillDelay * 2);

        }

        yield return new WaitForSeconds(refillDelay);
        ComboEffects();
        CascadeEffects();
        currentState = GameState.move; //allow player to move again.
        waitWall.SetActive(false);
    }


    //this seems to work.
    //!known issue - if you make 2 separate matches of 3, it comes up as a match-6 (instead of two match-3s)
    public void ComboEffects()
    {
        if (comboNumber > 1)
        {
            print("COMBO: " + comboNumber + "!!");
        }
        comboNumber = 0; //reset the chains/multiplier.
    }

    //these aren't working correctly atm
    public void CascadeEffects()
    {
        if (cascadesNum > 0)
        {
            print("CASCADES: " + cascadesNum + "!!");
        }
        cascadesNum = 0;

    }

    //to check for possible matches:
    public void SwitchPieces(int column, int row, Vector2 dir)
    {
        //save second piece in temp:
        GameObject holder = allDots[column+(int)dir.x, row+(int)dir.y] as GameObject;
        //switch first dot to be second position:
        allDots[column + (int)dir.x, row + (int)dir.y] = allDots[column, row];
        //set first dot to be the second dot:
        allDots[column, row] = holder;

    }

    //public bool CheckForMatches()
    //{
    //    for (int i = 0; i < width; i++)
    //    {
    //        for (int j = 0; j < height; j++)
    //        {
    //            if (allDots[i, j] != null)
    //            {
    //                if (i < width - 1)
    //                {

    //                }
    //            }
    //        }
    //    }
    //}

    public void MakingMatchOf(int colorNum, int matchedHowMany)
    {
        bool minionWasSpawned = false; //this will be switched off in easy mode, ex, if there are too many minions.
        if(myPlayer!=null){
            StatsManager.SM.matchesMade[myPlayer.playerNum, matchedHowMany]++;
            StatsManager.SM.IncrementAccountMatches(myPlayer.myPIN, colorNum, matchedHowMany);
            StatsManager.SM.IncrementStat(myPlayer.playerNum, 8);

            //StatsManager.SM.pStats[myPlayer.playerNum, 8]++;
            
            //print(StatsManager.SM.matchesMade[myPlayer.playerNum, matchedHowMany]);
            myPlayer.FireNotification(colorNum, matchedHowMany);
            //shakeshake for the playeR:
            if (myPlayer.rumbleOn)
            {
                StartCoroutine(MultiplayerWithBindingsExample.PlayerManager.PM.Vibrate(myPlayer.Device, (matchedHowMany / 10f), .3f));
            }
        }
        else{
            //print("CAN'T FIND PLAYER ON BOARD: " + name);
            StatsManager.SM.IncrementStat(1, 8);

        }
        //print("made a match of " + color);
        //print("spawning down " + myPlayer.laneIndex+1 + " lane");
        int lane = 0;

        if(myPlayer!=null){ //basically checking for autoplay or not:
            lane = myPlayer.laneIndex;
        }else{
            if(autoPlay)
            {
               //set it, but it will be modified based on difficulty:
                lane = Random.Range(0, myPaths.Count);

                switch (difficulty)
                {
                    case 0:
                        matchedHowMany = 3; //also make them weak.
                        goto default;
                        
                    case 1:
                        //don't research after level 2:
                        if(lane ==3 && ResearchLevel()>=2)
                        {
                            lane = Random.Range(0, 3);
                        }
                        break;
                    case 2:

                        goto default;
                    case 3:

                        goto default;
                    default:
                        //don't need to research on hard or super hard
                        //because there's logic in DestroyAuto that
                        //keeps the board constantly researching.
                        lane = Random.Range(0, 3);//3 = research; don't do that on easy mode.

                        break;
                }
            }
        }

        //!Add MEga Match functionality here:
        //Announcer sfx
        if (GameManager.GM.announcerOn && matchedHowMany>3)
        {
            GameManager.GM.PlayClip(GameManager.GM.matchAnnounceClips[matchedHowMany],(2f+matchedHowMany/5));
        }



        //limit the total number of enemies you're spawning
        //as easy AI
        //!!there might be something i have to implement to avoid droughts (long time with no spawning)

        if (autoPlay)
        {
            switch (difficulty)
            {
                case 0:
                    int numOfNonAuto = 0;
                    foreach(GameObject g in ObjectsManager.instance.enemyList2)
                    {
                        if(!g.name.Contains("Auto"))
                        {
                            numOfNonAuto++;
                        }
                    }
                    //print("non auto = " + numOfNonAuto);
                    if (numOfNonAuto < 4)
                    {
                        goto default;

                    }

                        break;
                case 1:

                    break;
                case 2:
                    //print("making it better by 1");
                    matchedHowMany += 1;

                    goto default;

                case 3:
                    matchedHowMany += 2;
                    goto default;

                default:
                    StartCoroutine(ObjectsManager.instance.SpawnEnemy(this, colorNum, myPaths[lane], lane, teamNum, matchedHowMany));
                    if (EventsManager.EM.doubleMatchNum == matchedHowMany)
                    {
                        StartCoroutine(SpawnOnDelay(colorNum, lane, matchedHowMany, .5f));
                        //ObjectsManager.instance.SpawnEnemy(this, colorNum, myPaths[lane], lane, teamNum, matchedHowMany);

                    }
                    minionWasSpawned = true;
                    break;

            }
           
        }else
        {
            StartCoroutine(ObjectsManager.instance.SpawnEnemy(this, colorNum, myPaths[lane], lane, teamNum, matchedHowMany));
            if (EventsManager.EM.doubleMatchNum == matchedHowMany)
            {
                StartCoroutine(SpawnOnDelay(colorNum, lane, matchedHowMany, .5f));
                //ObjectsManager.instance.SpawnEnemy(this, colorNum, myPaths[lane], lane, teamNum, matchedHowMany);

            }
            minionWasSpawned = true;

        }


        if(minionWasSpawned)
        {
            //!don't love this; not extendable.
            if (myPlayer == null)
            {
                PlayerShoot op = FindObjectOfType<PlayerShoot>();
                op.myLaneInfo.AddAMinion(colorNum, lane);
            }
            else
            {
                foreach (PlayerShoot ps in myPlayer.otherPlayers)
                {
                    ps.myLaneInfo.AddAMinion(colorNum, lane);
                }
            }
        }
       

    }

    //this is for event double spawning:
    public IEnumerator SpawnOnDelay(int colorNum, int lane, int matchedHowMany, float del)
    {
        yield return new WaitForSeconds(del);
        StartCoroutine(ObjectsManager.instance.SpawnEnemy(this, colorNum, myPaths[lane], lane, teamNum, matchedHowMany));

    }

    public void SpawnMega(int lane, int matchedHowMany)
    {
        ObjectsManager.instance.SpawnMegaMinion(this, myPaths[lane], lane, teamNum, matchedHowMany);

    }

    public int ResearchLevel()
    {
        if (!maxReasearch)
        {
            int num = 0;
            for (int i = monumentLevels.Count - 1; i > 0; i--)
            {
                if (monumentLevels[i].GetComponent<Collider>().enabled)
                {
                    num = i;
                }
            }
            return num;
        }
        else
        {
            return monumentLevels.Count;
        }
       
    }

    public void DestroyAuto(){
        if (GameManager.GM.playingGame && !GameManager.GM.gameIsEnded && autoPlay)
        {
            //for (int i = 0; i <= numToAutoKill; i++)
            //{

                //print("going through " + numToAutoKill);
                int rC = Random.Range(0, width);
                int rR = Random.Range(0, height);
                if (allDots[rC, rR] != null && !allDots[rC,rR].GetComponent<Gem>().isMatched)
                {
                    allDots[rC, rR].GetComponent<Gem>().TakeDamage(999, null);
                    //if(i==numToAutoKill){
                    //    //signal to the gem to wait (allows for multikilling):
                    //    doneAutoDest = true;
                    //    print("done auto");
                    //}
                }

            //in hard mode, every time you destroy a gem, if there's nobody
            //researching, research!
            if (!maxReasearch)
            {
                switch (difficulty)
                {
                    //make a check every 5 destroys in hard mode.
                    case 2:
                        autoResearchIndex++;
                        if (autoResearchIndex % 5 == 0)
                        {
                            CheckAndSpawnAResearcher();
                        }
                        break;
                    //just send one every destroy in super hard mode.
                    case 3:
                        CheckAndSpawnAResearcher();//will spawn a bunch cause it takes time to get there.
                        break;
                }

            }
            else
            {
                int l = Random.Range(0, 3);
                int col = Random.Range(0, GameManager.GM.actualColors.Length);
                autoMatchIndex++;

                switch (difficulty)
                {
                    //if you're maxed out in research, send a minion down lane every x destroys
                    case 2:
                        if (autoMatchIndex % 10 == 0)
                        {
                            //spawn a small minion
                            StartCoroutine(ObjectsManager.instance.SpawnEnemy(this, col, myPaths[l], l, teamNum, 3));
                            goto default;
                        }
                        break;
                    //just send one every destroy in super hard mode.
                    case 3:
                        //spawn a bigger minion
                        if (autoMatchIndex % 5 == 0)
                        {
                            StartCoroutine(ObjectsManager.instance.SpawnEnemy(this, col, myPaths[l], l, teamNum, Random.Range(4, 6)));
                            goto default;
                        }
                        break;
                    default:
                        PlayerShoot op = FindObjectOfType<PlayerShoot>();
                        op.myLaneInfo.AddAMinion(col, l);
                        break;
                }



            }
            //}

        }
    }

    public void CheckAndSpawnAResearcher()
    {
        MonumentLevel currentM = null;
        foreach (GameObject ml in monumentLevels)
        {
            if (ml.GetComponent<BoxCollider>().enabled)
            {
                currentM = ml.GetComponent<MonumentLevel>();
                //print("current research = " + currentM.name);
            }
        }
        if (currentM.unitsResearchingMe.Count == 0)
        {
            StartCoroutine(ObjectsManager.instance.SpawnEnemy(ObjectsManager.instance.boardsInplay[1], 2, myPaths[3], 3, 1, 3));
            //print("there's nobody researching ahhh");

        }
    }

    public void TurnOnLaneHighlight(int k)
    {
        for(int i=0; i<lanes.Length; i++)
        {
            for(int j=0; j<lanes[i].GetComponentsInChildren<ParticleSystem>().Length; j++)
            {
                if (i != k)
                {
                    lanes[i].GetComponentsInChildren<ParticleSystem>()[j].Stop();
                }
                else
                {
                    lanes[i].GetComponentsInChildren<ParticleSystem>()[j].Play();

                }
            }
        }
    }

    public void BlindAllGems(bool trueOrNah)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    allDots[i, j].GetComponent<Gem>().RiddleMe(trueOrNah);
                    allDots[i, j].GetComponent<Gem>().blinded = trueOrNah;
                }
            }
        }
    }

    public void GemBuffImageSwap(bool trueOn)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                allDots[i, j].GetComponent<Gem>().buffImg.enabled = trueOn;
            }
        }
    }

    [Button]
    public void SaveBoardState()
    {
#if UNITY_EDITOR
        ChallengeBoard cb = ScriptableObjectUtility.CreateAsset<ChallengeBoard>();
        cb.SetBoardState(GameManager.GM.challengeBoard);
#endif
    }

    public void OverWriteChallenge()
    {
        ChallengeBoard cb = GameManager.GM.cbs[GameManager.GM.cbIndex];
        cb.SetBoardState(GameManager.GM.challengeBoard);
    }

    public Vector3 MiddlePosition()
    {
        int mid = width / 2;
        Transform midGem = transform;
        foreach (Gem g in GetComponentsInChildren<Gem>())
        {
            if (g.column == mid && g.row == mid)
            {
                midGem = g.transform;
            }
        }
        return midGem.transform.position;
    }

    public int GetRandomItem(int noOfItems)
    {
        return noOfItems * (int)randomCurve.Evaluate(Random.value);
    }

    public void IncGemsDestroyedAndCheck(int color, int numDest)
    {
        //!remember to run a check for the gems that are off (Set them true)

        gemsDest[color] += numDest;
        UIManager.UIM.FrenzyFill(teamNum, color, gemsDest[color]);
        if (gemsDest[color] >= GameManager.GM.gFRandVals[color])
        {
            gFSatisfied[color] = true;
            UIManager.UIM.gFrenzHelpers[teamNum].checkmarks[color].enabled = true;
        }

        for (int i = 0; i < gFSatisfied.Length; i++)
        {
            if (!gFSatisfied[i])
            {
                return;
            }
            else
            {
                //print(GameManager.GM.possibleColors[i].name + " satisfied");
            }
        }
        if (teamNum == 0)
        {
            UIManager.UIM.EnableEndGame(1);

        }
        else
        {
            UIManager.UIM.EnableEndGame(0);

        }
    }

    [Button]
    public void SpawnGFrenzPickup()
    {
        GameObject go = Instantiate(gfParts, MiddlePosition(), Quaternion.identity);
        MoveTowardsUI mtu = go.GetComponent<MoveTowardsUI>();
        //mtu.cam = myPlayer.myCam;
        mtu.uiElement = UIManager.UIM.gFrenzHelpers[teamNum].progressTexts[0].rectTransform;
        //mtu.speed = 0.5f;
        //go.transform.SetParent(UIManager.UIM.gFrenzHelpers[teamNum].progressTexts[0].transform);
    }

}
