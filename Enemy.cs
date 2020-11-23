using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using NaughtyAttributes;

public class Enemy : MonoBehaviour
{
    public string buffedBy;
    public MeshRenderer[] colorCodeThese;
    public List<GameObject> points = new List<GameObject>();
    public int playerTeam;
    public int attackingTeam; //set by other enemy or by tower - semi-extendable for more than 2 teams.

    public float health; //health is going to go down, mx will not.
    float mxHealth;
    public float dmgToGive;
    public GameObject damageObjToThrow;
    public GameObject deathParticles, shieldDieParts, bubbShield;

    public bool canAttack = true;
    public float maxAttackDist;
    public float attackDelay;
    public int myColor;

    [Header("World UI Stuff")]
    public Canvas worldCan;
    public Canvas dieEmoteCan;
    public float emoteCanOffset;
    Image healthFill, statusEffect;
    public GameObject damagePrefab;
    public Board hBoard;
    public Sprite[] statusEmotes;
    public GameObject starHolder;
    public Animator minionAnim;
    public GameObject lightColumn;


    [SerializeField]
    Transform destination;

    public float distToTower;

    // Path and movement
    public float moveSpeed = 5f;
    public bool startMovement = false;
    public int pathIndex = 1;
    public int laneNum; //helpful for removing from the LaneInfoCanvas;
    public float rotateSpeed, rotateRad;
    public bool isStunned;
    public Tower towerToAttack;
    public Enemy enemyToAttack;
    public List<Enemy> enemiesInRange;
    public PlayerShoot pShootingMe;
    public bool isSlowed;
    public bool isInLava;
    public bool isDying; //yet another state...

    [Header("Bolts")]
    public GameObject boltToDrop;
    public int numOfBolts;

    [Header("Research Vars")]
    public float researchSpeed; //for jungling and building your 'wonder'
    public bool isResearching, waitingToBeSacd;
    public MonumentLevel monument;

    public int timesToGetPoisoned = 4;
    public float poisonPercentage = .05f;
    public float poisonDelay = 1f;
    public bool isPoisoned;
    float poisonDMG; //going to be based on % of maxHealth;
    int timesToGetPoisonedOG;
    public List<GameObject> hats = new List<GameObject>();
    public GameObject hatDad;

    public NavMeshAgent navMeshAgent;
    Coroutine lastStun;

    private void Start()
    {

        //navMeshAgent = GetComponent<NavMeshAgent>();
        mxHealth = health;
        worldCan = GetComponentInChildren<Canvas>();
        foreach (Transform t in worldCan.GetComponentsInChildren<Transform>())
        {
            if (t.name.Contains("Health"))
            {
                healthFill = t.GetComponent<Image>();
            }
            if (t.name.Contains("Emote"))
            {
                statusEffect = t.GetComponent<Image>();
            }
        }
        if (playerTeam == 0)
        {
            healthFill.color = Color.blue;
        }

        if (EventsManager.EM.lavaLane == laneNum)
        {
            InvokeRepeating("TakeLavaDamage", 1f, 1f);
        }
        poisonDMG = mxHealth * poisonPercentage;
        timesToGetPoisonedOG = timesToGetPoisoned;


        foreach(Transform t in GetComponentsInChildren<Transform>(true)){
            if(t.CompareTag("Hat")){
                hatDad = t.gameObject;
            }
            if (t.CompareTag("BubbleShield"))
            {
                bubbShield = t.gameObject;
            }
        }
        if(GameManager.GM.hatsOn)
        {
            TurnOnHat();
        }else{
            hatDad.SetActive(false); //turn off the parent go, which has a collider.
        }
        //add your buffs when you startup:
        AddTeamBuffs(playerTeam);
        
        SpawnLightColumn();
        
        
    }

    void Update()
    {
        worldCan.transform.rotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, hBoard.transform.eulerAngles.y, transform.eulerAngles.z));

        if (!GameManager.GM.everythingPause)
        {

            if (isResearching)
            {
                Research();
            }

            if (startMovement && !isStunned && !isDying)
            {
                navMeshAgent.isStopped = false;
                FollowPath();
                SearchForTarget();
            }
            else
            {
                if(navMeshAgent.enabled)
                navMeshAgent.isStopped = true;

            }

            if (canAttack)
            {
                Attack();
            }

            //if you ever have no target, just start moving along your path again:
            if (towerToAttack == null && enemyToAttack == null && !isResearching && !waitingToBeSacd)
            {
                startMovement = true;
                enemiesInRange.Clear();

                //navMeshAgent.isStopped = false;
            }

        }
        else
        {
            //turn it off during events, etc so it doesn't freak out
            //(it does freak out while attacking but it looks cool.)
            if (navMeshAgent.enabled&&!navMeshAgent.isStopped)
            {

                navMeshAgent.isStopped = true;
                //navMeshAgent.enabled = false;
            }
        }

        //Debug killing:
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    DeathStuff();
        //}
    }

    private void SearchForTarget()
    {

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxAttackDist);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].CompareTag("Tower"))
            {
                Tower dTower = hitColliders[i].GetComponent<Tower>();
                if (dTower.towerNum != playerTeam) //if you encounter a tower that's not your team num
                {
                    if (towerToAttack == null)
                    {
                        startMovement = false;
                        towerToAttack = hitColliders[i].GetComponent<Tower>();
                        canAttack = true;
                        //navMeshAgent.isStopped = true; //i don't know if this is stopping the character or not. - EG
                    }

                }
            } else
            if (hitColliders[i].CompareTag("Minion"))
            {
                Enemy e = hitColliders[i].GetComponent<Enemy>();
                if (e.playerTeam != playerTeam && !enemiesInRange.Contains(e))
                {
                    startMovement = false;
                    enemiesInRange.Add(e);
                    canAttack = true;

                }
            }
        }

        enemyToAttack = ClosestEnemy();


    }
    private void Research()
    {
        if (!waitingToBeSacd)
        {


            minionAnim.SetBool("researching", isResearching);
            transform.RotateAround(monument.transform.position, Vector3.up, rotateSpeed * Time.deltaTime);
            transform.RotateAround(monument.transform.position, Vector3.right, moveSpeed * Time.deltaTime);
            Vector3 desiredPos = (transform.position - monument.transform.position).normalized * rotateRad + monument.transform.position;

            transform.position = Vector3.MoveTowards(transform.position, desiredPos, Time.deltaTime * rotateSpeed);


            monument.ProgressUp(researchSpeed);
            //monument.expProgress += researchSpeed * Time.deltaTime;
            //if (monument.expProgress >= monument.expToResearch)
            //{
            //    monument.SetEnemies();
            //    //monument.LevelUp(playerTeam);
            //}
        }
    }
    public Enemy ClosestEnemy()
    {
        if (enemiesInRange.Count > 0)
        {
            enemiesInRange.Sort((x, y) => Vector3.Distance(transform.position, x.transform.position).CompareTo(Vector3.Distance(transform.position, y.transform.position)));

            return enemiesInRange[0];

        }
        else
        {
            return null;
        }
        //float dist = 9999f;
        //foreach(Enemy e in enemiesInRange)
        //{
        //    if(Vector3.Distance(transform.position, e.transform.position) < dist)
        //    {
        //        dist = Vector3.Distance(transform.position, e.transform.position);
        //        return e;
        //    }
        //}
    }


    public void AddTeamBuffs(int t)
    {
        TeamBuffs tb = GameManager.GM.teamBuffs[t];
        moveSpeed += tb.m_moveSpeed;
        attackDelay -= tb.m_attackSpeedMod;
        dmgToGive += tb.m_damageBuff;
        researchSpeed *= EventsManager.EM.researchRate; //if the research event, then base research rate gets doubled.
        researchSpeed += tb.m_researchSpeed;
        maxAttackDist += tb.m_range;
        health += tb.m_health;

        //turn off shield unless it has been unlocked:
        if (!tb.shieldsOn)
        {
            bubbShield.SetActive(false);
        }
    }

    public void GetPath(GameObject pathD)
    {
        //add all the points from the childed GOs of the parent path:
        foreach (Transform t in pathD.GetComponentsInChildren<Transform>())
        {
            if (t.gameObject != pathD)
                points.Add(t.gameObject);
        }
        //sort 'em by name
        points.Sort((x, y) => x.name.CompareTo(y.name));
        SetDest(points[pathIndex]);
        //points = ObjectsManager.instance.paths[pathNum - 1];
    }

    public void GetTowerToAttack(int towerNum)
    {
        /*
        if (towerNum == 1)
            towerToAttack = ObjectsManager.instance.tower2;
        else if (towerNum == 2)
            towerToAttack = ObjectsManager.instance.tower1;
        */
    }

    // Deprecated
    public void FollowPath()
    {

        if (vector2DDistance(transform.position, points[pathIndex].transform.position) > 0.2f)
        {
            //transform.position = Vector3.MoveTowards(transform.position, points[pathIndex].transform.position, moveSpeed * Time.deltaTime);
            //transform.LookAt(points[pathIndex].transform); //dirty but for now this should work to get the enemy looking where it's going!
        }
        else
        {
            if (pathIndex < points.Count - 1)
            {
                pathIndex++;
                SetDest(points[pathIndex]);
                //print("PATH INDEX INCREMENTED AND CALLED SET DEST");
            }
            else
            {
                startMovement = false;
            }


        }
    }

    //from https://forum.unity.com/threads/ignore-y-axis-difference-in-vector3-distance-calc-execution-time.335796/
    private float vector2DDistance(Vector3 v1, Vector3 v2)
    {
        float xDiff = v1.x - v2.x;
        float zDiff = v1.z - v2.z;
        return Mathf.Sqrt((xDiff * xDiff) + (zDiff * zDiff));
    }

    // Sets the destination for the nav mesh agent
    void SetDest(GameObject target)
    {
        navMeshAgent.speed = moveSpeed;

        navMeshAgent.SetDestination(target.transform.position);
        //print("new navmesh target = " + target);

    }

    public void TakeDamage(float dmgAmt, int attTeam, bool showDamage)
    {
        health -= dmgAmt;
        attackingTeam = attTeam;
        healthFill.fillAmount = health / mxHealth;
        
        if (showDamage)
        {
            SpawnDamageCan(dmgAmt);

            //Text t = Instantiate(damagePrefab, transform, false).GetComponentInChildren<Text>();
            //string mod = "-";
            //if (dmgAmt < 0)//if you're sending in a negative number, act like you're adding health
            //{
            //    mod = "+";
            //}
            //t.text = mod + (int)Mathf.Abs(dmgAmt);
            ////t.transform.Rotate(transform.forward);
            //t.transform.rotation = worldCan.transform.rotation;
        }

        if (health <= 0)
        {

            DeathStuff();
        }
        else
        {
            if (minionAnim != null)
                minionAnim.SetTrigger("Hit");
        }
    }
    public void TakeDamage(float dmgAmt, PlayerShoot p)
    {

        health -= dmgAmt;

        attackingTeam = p.teamNum;

        healthFill.fillAmount = health / mxHealth;

        SpawnDamageCan(dmgAmt);

        //Text t = Instantiate(damagePrefab, transform, false).GetComponentInChildren<Text>();
        //string mod = "-";


        //if (dmgAmt < 0)//if you're sending in a negative number, act like you're adding health
        //{
        //    mod = "+";
        //}
        if (pShootingMe == null || pShootingMe != p)
        { //last hit functionality
            pShootingMe = p;
        }
        //t.text = mod + (int)Mathf.Abs(dmgAmt); //had to abs it cause it was reading "+-10",
        //t.transform.Rotate(transform.forward);
        if (health <= 0)
        {

            DeathStuff();
            //update Stats:
            //StatsManager.SM.pStats[p.playerNum, 1]++;
            //!!atm the computer is not logging (or doing) minion kills.
            StatsManager.SM.IncrementStat(p.playerNum, 1);
            Instantiate(dieEmoteCan, transform.position+(Vector3.up* emoteCanOffset), p.myBoard.transform.rotation);
            GameManager.GM.PlayClip(17, .3f);

            if (pShootingMe != null)
            {
                DropBolts(); //double bolts for last hit?
            }
        }
        else
        {
            if (lastStun != null) StopCoroutine(lastStun);
            lastStun = StartCoroutine(GetStunned(GetClipTime(minionAnim,"Hit")));
            if (minionAnim != null)
                minionAnim.SetTrigger("Hit");
        }
        

    }

    [Button]
    public void DeathStuff()
    {
        ObjectsManager.instance.RemoveMe(gameObject);

        isDying = true;
        if (minionAnim != null)
            minionAnim.SetTrigger("Die");

        if (monument != null)
        {
            if (!hBoard.autoPlay)
            {

                //print("calling remove");
                monument.RemoveAndCheck(this, playerTeam);
            }
        }
        else
        {
            //instantiate some bolts:

            DropBolts();
        }


        //update sm:
        StatsManager.SM.minionKills[MultiplayerWithBindingsExample.PlayerManager.PM.players.IndexOf(pShootingMe) + 1]++;
        GameManager.GM.PlayRandomDeathClip();
        Instantiate(deathParticles, transform.position, Quaternion.identity);


        //!not a fan of this; not extendable.
        if (hBoard.myPlayer == null)
        {
            PlayerShoot op = FindObjectOfType<PlayerShoot>();

            op.myLaneInfo.RemoveAMinion(myColor, laneNum);
        }
        else
        {
            //also remove yourself from other player's info:
            foreach (PlayerShoot ps in hBoard.myPlayer.otherPlayers)
            {
                if (ps.myLaneInfo != null)
                {
                    ps.myLaneInfo.RemoveAMinion(myColor, laneNum);
                }
            }
        }
        if (minionAnim != null)
        {
            foreach(Collider c in GetComponents<Collider>())
            {
                c.enabled = false;
            }

            Destroy(gameObject, GetClipTime(minionAnim, "die")+0.2f); //let it linger a bit so it plays the whole animation before dying.
            


        }
        else
        {
            Destroy(gameObject);

        }
    }

    public void DropBolts()
    {
        for (int i = 0; i < numOfBolts; i++)
        {
            GameObject b = Instantiate(boltToDrop, transform.position, Quaternion.identity);
            b.tag = "Bolt" + attackingTeam;
            //give it a bit of a random explosion vector:
            float randX = Random.Range(-.8f, .8f);
            float randZ = Random.Range(-.8f, .8f);
            Vector3 randDir = new Vector3(randX, 2, randZ);
            b.GetComponent<Rigidbody>().AddForce(randDir * 300f);
            //GameManager.GM.boltsOnField[attackingTeam]++;
            b.GetComponent<BoltEffects>().SetMyVars(attackingTeam);
            b.GetComponent<BoltEffects>().Invoke("DestroyMe", 20);//make it destroy itself and decrement after a while.

            //Destroy(b, 20f);
        }
    }

    public void Attack()
    {
        //rn, this prioritizes towers over enemies:
        if (towerToAttack != null && towerToAttack.isAlive)
        {
            

            //float distToTower = Vector3.Distance(transform.position, towerToAttack.transform.position);
            //float distToLastPt = Vector3.Distance(transform.position, points[points.Count - 1].transform.position);

            //if (distToTower <= maxAttackDist && distToLastPt <= 1f)
            //{

            towerToAttack.attacker = gameObject;
            towerToAttack.TakeDamage(dmgToGive);
            Transform top = null;
            foreach (Transform t in towerToAttack.GetComponentsInChildren<Transform>())
            {
                if (t.name.Contains("Top"))
                {
                    top = t;
                }
            }
            StartCoroutine(AttackDelay());
            if (top != null)
            {
                SendDamageOBJ(top);
            }
            else
            {
                SendDamageOBJ(towerToAttack.transform);
                print("!!Can't find tower top");
            }
            Debug.DrawLine(transform.position, towerToAttack.transform.position, GameManager.GM.actualColors[myColor], .3f);
            //}

        }
        else if (enemyToAttack != null)
        {
            //enemyToAttack.enemyToAttack = this; //draw aggro?  this may be dumb.
            enemyToAttack.TakeDamage(dmgToGive, playerTeam, true);
            StartCoroutine(AttackDelay());
            SendDamageOBJ(enemyToAttack.transform);

            Debug.DrawLine(transform.position, enemyToAttack.transform.position, GameManager.GM.actualColors[myColor], .3f);
            

        }
    }

    public void SendDamageOBJ(Transform destination) {
        GameObject g = Instantiate(damageObjToThrow, transform.position, Quaternion.identity);
        g.transform.SetParent(destination); //the script on this object will make it go to its parent then die
    }

    IEnumerator AttackDelay()
    {
        canAttack = false;

        yield return new WaitForSeconds(attackDelay);

        canAttack = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Research"))
        {
            if (!isResearching && !other.GetComponent<MonumentLevel>().waitingForSacrifice)
            {
                startMovement = false;
                GetComponent<NavMeshAgent>().enabled = false;
                GetComponent<NavMeshObstacle>().enabled = false;
                GetComponent<Collider>().enabled = false;
                healthFill.transform.parent.gameObject.SetActive(false); //this sets the health bar bg inactive.
                //Destroy(GetComponent<Collider>());
                monument = other.GetComponent<MonumentLevel>();
                monument.unitsResearchingMe.Add(this);
                isResearching = true;
            } else if (other.GetComponent<MonumentLevel>().waitingForSacrifice && !waitingToBeSacd)
            {
                Destroy(gameObject);
            }
        }
        if (other.CompareTag("MaxResearch"))
        {
            DeathStuff();//this will grant bolts on death, which 
            //might be fine since you maxed out research and it's
            //time to end the game.
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, maxAttackDist);
    }

    /// <summary>
    /// ex. match-4 = +5 health, +1dmg, -0.1fattackDelay.
    /// </summary>
    /// <param name="matchesMade"></param>
    public void AddMatchBuffs(int matchesMade)
    {
        //more gems = more powerful:
        //this should be more advanced and balanced and all that good game design stuff, but for now...
        health += (15 * matchesMade);
        dmgToGive += (2.5f * matchesMade);
        attackDelay -= matchesMade / 10;
        moveSpeed += (float)matchesMade / 1.5f;
        if (attackDelay < .5f)
        {
            attackDelay = .5f;
        }
        SetupStarLevels(matchesMade);
    }

    public IEnumerator GetSlowed() {
        float temp = moveSpeed;
        moveSpeed /= 2;
        isSlowed = true;
        yield return new WaitForSeconds(3f);
        moveSpeed = temp;
        isSlowed = false;
    }

    public IEnumerator GetPoisoned()
    {
        if (timesToGetPoisoned > 0)
        {
            isPoisoned = true;
            TakeDamage(poisonDMG, OpposingTeam(), true);
        }
        else
        {
            isPoisoned = false;
            timesToGetPoisoned = timesToGetPoisonedOG;
            StopCoroutine(GetPoisoned());
            yield break;
        }
        yield return new WaitForSeconds(poisonDelay);
        timesToGetPoisoned--;
        StartCoroutine(GetPoisoned());

    }

    public bool IsAtMaxHealth()
    {
        if (health >= mxHealth)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void PostResearchSet()
    {
        isResearching = false;
        minionAnim.SetBool("researching", isResearching);
        waitingToBeSacd = true;
        GetComponent<Collider>().enabled = true;
        //also set your status effect to look like you're waiting to die.
        GetComponent<FloatyMovement>().enabled = true;
        GetComponent<UnityStandardAssets.Utility.AutoMoveAndRotate>().enabled = true;
    }

    public void TakeLavaDamage()
    {
        if (EventsManager.EM.lavaLane == laneNum)
        {
            isInLava = true;
            TakeDamage(EventsManager.EM.lavaDmg, OpposingTeam(), false);
        } else {
            isInLava = false;
            CancelInvoke("TakeLavaDamage"); //otherwise, stop taking damage (if at end of event, ex.)
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Turret"))
        {
            TakeDamage(EventsManager.EM.turretDmg, OpposingTeam(), false);
        }
    }

    int OpposingTeam()
    {
        int ot = 999;
        if (playerTeam == 0)
        {
            ot = 1;
        }
        else
        {
            ot = 0;
        }
        return ot;
    }
    void SetupStarLevels(int howManyOn)
    {
        Image[] stars = starHolder.GetComponentsInChildren<Image>();
        System.Array.Sort(stars, (x, y) => x.name.CompareTo(y.name));
        for(int i=howManyOn; i<stars.Length; i++)
        {
            stars[i].enabled = false;
        }
    }

    public void TurnOnHat()
    {
        if (hBoard.myPlayer != null && hBoard.myPlayer.chosenHat>=0)
        {
            foreach (Transform t in hatDad.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag("HatChild") && !hats.Contains(t.gameObject))
                {
                    hats.Add(t.gameObject);
                }
            }

            for (int i = 0; i < hats.Count; i++)
            {
                if (i == hBoard.myPlayer.chosenHat)
                {
                    hats[i].SetActive(true);
                }
                else
                {
                    hats[i].SetActive(false);
                }
            }
        }

    }

    float GetClipTime(Animator anim, string name)
    {
        float time = 0;
        foreach(AnimationClip c in anim.runtimeAnimatorController.animationClips)
        {
            if(c.name == name)
            {
                time = c.length;
            }
        }
        return time;
    }

    public void ColorCodeMe(int color)
    {
        foreach(MeshRenderer mr in colorCodeThese)
        {
            //print("assigning " + GameManager.GM.possibleColors[color] + " to " +mr.materials[0]);
            mr.material = GameManager.GM.possibleColors[color];
            //mr.materials[0] = null;
            
        }
    }
    public IEnumerator GetStunned(float time)
    {
        isStunned = true;
        yield return new WaitForSeconds(time);
        isStunned = false;
    }

    public void SpawnDamageCan(float dmg){
        for (int i = 0; i < 2; i++)
        {
            Text t = Instantiate(damagePrefab, transform, false).GetComponentInChildren<Text>();
            string mod = "-";
            if (dmg < 0)//if you're sending in a negative number, act like you're adding health
            {
                mod = "+";
            }
            t.text = mod + (int)Mathf.Abs(dmg);
            //t.transform.Rotate(transform.forward);
            //this hides it from the opposing player's camera:
            t.transform.parent.gameObject.layer = LayerMask.NameToLayer("DamageNumbersP" + i);

            if (playerTeam == 0)
            {
                t.color = Color.blue;

            }
            else
            {
                t.color = Color.red;

            }



            //There is probably a much easier, nicer way to do this but
            //my eyeball is starting to throb
            //Vector3 rrot = new Vector3(0, i, 0);
            //t.transform.parent.localRotation = Quaternion.Euler(rrot * 180);
            if (i==0){
                //t.transform.Rotate(transform.forward);
                if(playerTeam==0){
                    //t.transform.parent.localRotation = Quaternion.identity;
                    t.transform.parent.rotation = ObjectsManager.instance.boardsInplay[1].transform.rotation;

                }else{
                    //t.transform.parent.localRotation = Quaternion.Euler(Vector3.up * 180);
                    t.transform.parent.rotation = ObjectsManager.instance.boardsInplay[1].transform.rotation;

                }


            }
            else
            {
                if(playerTeam==0)
                {
                    //t.transform.parent.localRotation = Quaternion.Euler(Vector3.up * 180);
                    t.transform.parent.rotation = ObjectsManager.instance.boardsInplay[0].transform.rotation;


                }
                else
                {
                    //t.transform.parent.localRotation = Quaternion.identity;
                    t.transform.parent.rotation = ObjectsManager.instance.boardsInplay[0].transform.rotation;


                }
            }


        }
    }
    public void SpawnLightColumn()
    {
        GameObject lc = Instantiate(lightColumn, transform);
        Color c = Color.white;
        if (!name.Contains("Auto"))
        {
            c = GameManager.GM.actualColors[myColor];
        }
        lc.GetComponent<MeshRenderer>().material.color = c;
        foreach (ParticleSystem ps in lc.GetComponentsInChildren<ParticleSystem>())
        {
            ps.startColor = c;
        }
        Destroy(lc, 3f);
    }

    public void BurstBubble()
    {
        Instantiate(shieldDieParts, transform.position, Quaternion.identity);
        bubbShield.SetActive(false);
    }
}
