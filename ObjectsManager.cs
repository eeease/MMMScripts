using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectsManager : MonoBehaviour
{
    public static ObjectsManager instance;

    public List<GameObject> enemyList1 = new List<GameObject>();
    public List<GameObject> enemyList2 = new List<GameObject>();
    public List<Tower> allTowers;
    public List<Board> boardsInplay;
    public GameObject pathsObj;
    public List<GameObject> rPaths;
    public List<GameObject> bPaths;
    public List<List<Vector3>> paths = new List<List<Vector3>>();
    List<Vector3> pts;

    public Text tower1Txt, tower2Txt;
    public Tower rCore, bCore;

    public GameObject enemyHolder1, enemyHolder2;
    public List<GameObject> enemiesToSpawn;
    public List<Attractor> vacsInGame; //this will be housed here to make bolt adding/subtracting easier.
    public GameObject[] laneTurrets;
    public List<GameObject> allTargets;

    public GameObject eSpawnParts;

    private void Awake()
    {
        instance = this;

        GeneratePaths();
    }
    private void Start()
    {
        foreach(Board b in FindObjectsOfType<Board>())
        {
            boardsInplay.Add(b);
        }
        boardsInplay.Sort((x, y) => x.name.CompareTo(y.name));

        foreach(Attractor a in FindObjectsOfType<Attractor>()){
            vacsInGame.Add(a);
        }
        vacsInGame.Sort((x, y) => x.name.CompareTo(y.name));

        //this includes cores right now.  not sure if we want to remove them:
        foreach(Tower t in FindObjectsOfType<Tower>())
        {
            allTowers.Add(t);
        }
    }

    private void Update()
    {
        ////!maybe this should show total health of all towers per side?
        //tower1Txt.text = "Red Core: " + rCore.health;
        //tower2Txt.text = "Blue Core: " + bCore.health;

        //if either tower goes down, other player wins:
        //if (rCore.health <= 0 || bCore.health <=0)
        //{
        //    //!obviously have to do some more resplendant victory stuff here.
        //    GameManager.GM.gameIsEnded = true;
        //}
        
    }

    void GeneratePaths()
    {
        // Loop through each path obj
        foreach (Transform pathPar in pathsObj.transform)
        {
            // Create a temp List of Vector3 that will hold all points for a path
            List<Vector3> pts = new List<Vector3>();

            // Loop through each path point and add it to the temp list
            foreach (Transform pathPt in pathPar.transform)
                pts.Add(pathPt.position);

            // Add the temp list to the list of paths
            paths.Add(pts);

            // Debug the path with a line
            DrawDebugPath(pts);
        }
    }

    void DrawDebugPath(List<Vector3> pts)
    {
        //Debug.Log(pts.Count);

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Debug.DrawLine(pts[i], pts[i+1], Color.red, Mathf.Infinity, false);
        }
    }

    public IEnumerator SpawnEnemy(Board homeBoard, int colorOfGem, GameObject pathDad, int lane, int team, int mHM)
    {
        //this is being called from 7 places, so i might just add the gem frenzy logic here...
        if (GameManager.GM.gameMode == 1)
        {
            homeBoard.IncGemsDestroyedAndCheck(colorOfGem, mHM);
        }
        else
        {



            //mHM = how many have been matched to spawn this enemy.  Good to know for modifying its power, etc.

            //spawn some particles up into the air:
            GameObject p = Instantiate(eSpawnParts, homeBoard.MiddlePosition(), Quaternion.LookRotation(transform.up));
            p.GetComponentInChildren<TrailRenderer>().startColor = GameManager.GM.actualColors[colorOfGem];
            p.GetComponentInChildren<ParticleSystem>().startColor = GameManager.GM.actualColors[colorOfGem];
            if (colorOfGem == 3)
            {
                p.GetComponentInChildren<ParticleSystem>().startColor = Color.white; //black is not visible against the skybox.

            }
            p.GetComponent<ParticlesUpThenTarget>().target = pathDad.transform;

            yield return new WaitForSeconds(p.GetComponent<ParticlesUpThenTarget>().del * 2);

            GameObject spawnedEnemy = Instantiate(enemiesToSpawn[colorOfGem], pathDad.transform.position, Quaternion.identity); ;
            Enemy e = spawnedEnemy.GetComponent<Enemy>(); //it's better to get the component once than getting it 3-5 times in succession:
            e.AddMatchBuffs(mHM - 3);
            float rx = e.transform.localScale.x + ((mHM - 3) / 5f); //have to do 
                                                                    //float xmod = (mHM - 3) / 5f;
            e.transform.localScale = Vector3.one * rx;
            //e.moveSpeed += rx/2;
            e.GetPath(pathDad);
            e.playerTeam = team;
            e.hBoard = homeBoard;
            //e.GetTowerToAttack(playerNum);
            e.myColor = colorOfGem;
            e.ColorCodeMe(colorOfGem);
            e.laneNum = lane;
            if (lane != 3) //researching minions aren't added to the enemy lists.
            {
                AddMe(e);
            }
            //if (team == 1)
            //{
            //    spawnedEnemy.transform.parent = enemyHolder1.transform;
            //    enemyList1.Add(spawnedEnemy);
            //}
            //else if (team == 2)
            //{
            //    spawnedEnemy.transform.parent = enemyHolder2.transform;
            //    enemyList2.Add(spawnedEnemy);
            //}
            if (GameManager.GM.minionDiff)
            {
                ApplyColorSpecificBuffs(e, colorOfGem, homeBoard, mHM);
            }
            e.startMovement = true;
        }
        //update stats:
        if(homeBoard.myPlayer!=null)
        {
            StatsManager.SM.IncrementStat(homeBoard.myPlayer.playerNum, 0);

        }else{
            StatsManager.SM.IncrementStat(1, 0);

        }


    }

    public void ApplyColorSpecificBuffs(Enemy en, int color, Board teamBoard, int mHM)
    {
        switch (color)
        {
            //blue
            case 0:
              
                //mod it by kills:
                int mod = StatsManager.SM.pStats[teamBoard.teamNum, 1];
                if (mod > 50)
                {
                    mod = 50;
                }
                en.dmgToGive += mod / 5;
                en.health += mod / 5;

                break;

            //gold
            case 1:
                int curBolts = GameManager.GM.teamBolts[teamBoard.teamNum];
                //clamo curBolts based on the type of match:
                if (curBolts > (mHM * 10))
                {
                    curBolts = (mHM * 10);
                }

                en.dmgToGive += curBolts / 10;
                en.health += curBolts / 10;

                break;

            //green
            case 2:
                //buffed according to your research level
                //en.health += teamBoard.ResearchLevel()*10;
                en.maxAttackDist += teamBoard.ResearchLevel();

                //buff research by how many teammates i have in the field:
                if (teamBoard.teamNum == 0)
                {
                    en.researchSpeed += enemyList1.Count;

                }
                else
                {
                    en.researchSpeed += enemyList2.Count;
                }
                break;

            //black
            case 3:
                //buffed by number of minions the player has
                //or should it be total number of minions in lanes?
                en.health += (enemyList1.Count + enemyList2.Count);


                break;
            //purp
            case 4:
                //buffed according to how many items you have bought:
                if (teamBoard.myPlayer != null)
                {
                    en.dmgToGive += teamBoard.myPlayer.purchasedItems.Count;
                    en.moveSpeed += teamBoard.myPlayer.purchasedItems.Count / 2;
                }
                //!gotta maybe fix for single player?  (computer purchases no items)
                break;

            //red
            case 5:
                //buffed according to how much swap juice you have.
                if (teamBoard.myPlayer != null)
                {

                    en.dmgToGive += teamBoard.myPlayer.sJuice / 10; //if swapjuice max is 100, max dmg buff = 10.
                    en.attackDelay -= teamBoard.myPlayer.sJuice / 200; //current delay = 1, so max benefit is -0.5f
                }
                break;

        }
    }

    public void SpawnMegaMinion(Board b, GameObject pathDad, int lane, int team, int buffMod)
    {
        GameObject spawnedEnemy = Instantiate(enemiesToSpawn[10], pathDad.transform.position, Quaternion.identity); ;
        Enemy e = spawnedEnemy.GetComponent<Enemy>(); //it's better to get the component once than getting it 3-5 times in succession:
        e.GetPath(pathDad);
        e.playerTeam = team;
        e.hBoard = b;
        //e.GetTowerToAttack(playerNum);
        e.AddMatchBuffs(buffMod);
        e.transform.localScale *= 1+(buffMod / 5f);

        //e.transform.localScale = new Vector3(1, (1 + ((buffMod/5f))), 1);

        e.laneNum = lane;
        AddMe(e);
        //if (team == 1)
        //{
        //    spawnedEnemy.transform.parent = enemyHolder1.transform;
        //    enemyList1.Add(spawnedEnemy);
        //}
        //else if (team == 2)
        //{
        //    spawnedEnemy.transform.parent = enemyHolder2.transform;
        //    enemyList2.Add(spawnedEnemy);
        //}
        e.startMovement = true;

    }

    public void SpawnDownAllLanes()
    {

            foreach (Board b in boardsInplay)
            {
                if (b.myPlayer != null)
                    b.myPlayer.FireNotification("Auto Minions Spawned", Color.white);
            }
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {


                GameObject spawnedEnemy = Instantiate(enemiesToSpawn[10], boardsInplay[i].myPaths[j].transform.position, Quaternion.identity); ;
                Enemy e = spawnedEnemy.GetComponent<Enemy>(); //it's better to get the component once than getting it 3-5 times in succession:
                e.GetPath(boardsInplay[i].myPaths[j]);
                e.playerTeam = i;
                e.hBoard = boardsInplay[i];
                e.laneNum = j;
                e.AddMatchBuffs(0); //this is just shutting off the stars in starholder for auto lane minions.

                //e.GetTowerToAttack(playerNum);
                //e.myColor = Color.grey;
                AddMe(e); //for now i'm not going to add the auto-minions
                //if (i == 0)
                //{
                //    spawnedEnemy.transform.parent = enemyHolder1.transform;
                //    enemyList1.Add(spawnedEnemy);
                //}
                //else if (i == 1)
                //{
                //    spawnedEnemy.transform.parent = enemyHolder2.transform;
                //    enemyList2.Add(spawnedEnemy);
                //}
                e.startMovement = true;

            }
        }

    }

    public void AddMe(Enemy e)
    {
        switch (e.playerTeam)
        {
            case 0:
                enemyList1.Add(e.gameObject);
                e.transform.SetParent(enemyHolder1.transform);

                break;
            case 1:
                enemyList2.Add(e.gameObject);
                e.transform.SetParent(enemyHolder2.transform);

                break;
        }
    }

    public void RemoveMe(GameObject e)
    {
        if (enemyList1.Contains(e))
        {
            enemyList1.Remove(e);
        }
        if (enemyList2.Contains(e))
        {
            enemyList2.Remove(e);
        }
    }

    public void SetAIDifficulty(int whaaat)
    {
        //i'm like 98% sure the AI is always going to be [1]
        boardsInplay[1].difficulty = whaaat;
    }

}
