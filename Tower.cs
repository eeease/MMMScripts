using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;


public class Tower : MonoBehaviour
{

    public int towerNum;
    public string myLane;
    public float health, mxHealth;

    public float maxShootDist, maxShootAdd;

    public float dmgToGive, dmgAdd;

    public float shootDelay, delMod;

    public GameObject projectilePrefab;

    bool canShoot = true;
    bool canTakeDmg = true;
    public bool isAlive = true;
    Transform myTop;
    public GameObject iceCube;
    public bool isFrozen;

    // The current enemy attacking the tower
    public GameObject attacker;

    LineRenderer lineRend;
    public GameObject deathParticles;
    public float deathDelay = 2f;
    [Header("Bolts")]
    public GameObject boltToDrop;
    public int numOfBolts;

    [Header("Canvas Info Vars")]
    public Image statusImage;
    public Sprite hurtSprite, deadSprite;
    public Image healthIMG; //this is 0 to 1;
    public TowerInfoHelper uiInfo;


    [Header("Supercharge Variables")]
    public bool isSupercharged;
        public bool beingCharged;
    public Tower nextTower;
    public float sc, scOG, timeToBeCharged, scMult, diminCD,diminCDOG;
    public ParticleSystem chargeParts;

    [Header("Core vars")]
    public bool at50, at25;
    public AudioClip[] myColor; //0=blue

    //!still need to add the actual changes from being SuperCharged (deal more damage, faster attack speed, etc)

    private void Awake()
    {
        lineRend = transform.GetChild(1).GetComponent<LineRenderer>();
        chargeParts = GetComponent<ParticleSystem>();
        statusImage.enabled = false;
        health = mxHealth;
        foreach(Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag("TowerTop"))
            {
                myTop = t;
            }
        }
    }

    private void Update()
    {
        if (canShoot && !GameManager.GM.everythingPause && !isFrozen)
            Shoot();

        //DrawLine();



        if (diminCD > 0)
        {
            diminCD -= Time.deltaTime;
        }
        else
        {
            beingCharged = false;
        }
        if(!beingCharged)
        DiminishCharge();
    }

    public void TakeDamage(float dmgAmt)
    {
        if (canTakeDmg)
        {
            health -= dmgAmt;
            healthIMG.fillAmount = health / mxHealth;
            StopCoroutine(DamageBlink(3));
            StartCoroutine(DamageBlink(3));
            if(MultiplayerWithBindingsExample.PlayerManager.PM.players.Count-1>=towerNum &&
               MultiplayerWithBindingsExample.PlayerManager.PM.players[towerNum]!=null)
            MultiplayerWithBindingsExample.PlayerManager.PM.players[towerNum].FlashUnderAttack(myLane);

            if (uiInfo != null)
            {
                uiInfo.SetFill(health / mxHealth);
            }
        }
        if (name.Contains("CORE"))
        {
            UIManager.UIM.coreHealthText[towerNum].text = ((int)health).ToString();
            if (health <= 0)
            {
                UIManager.UIM.EnableEndGame(towerNum);
                
            }
            //announcer for core health checkpoints:
            if (GameManager.GM.announcerOn)
            {
                if(health <= mxHealth / 2 && !at50)
                {
                    GameManager.GM.PlayClip(myColor[towerNum], GameManager.GM.SFXVol()*5);
                    StartCoroutine(GameManager.GM.PlayClipOnDelay(GameManager.GM.matchAnnounceClips[10], 1, .7f));

                    at50 = true;
                }
                if(health<=mxHealth/4 && !at25)
                {
                    GameManager.GM.PlayClip(myColor[towerNum], GameManager.GM.SFXVol()*5);
                    StartCoroutine(GameManager.GM.PlayClipOnDelay(GameManager.GM.matchAnnounceClips[11], 1, .7f));
                    at25 = true;
                }

            }
        }



        if (health <= 0 && isAlive)
        {
            int otherTeam = 999;
            //don't love this but hell:
            if(towerNum==0)
            {
                otherTeam = 1;
            }else{
                otherTeam = 0;
            }
            //Debug.Log("Tower destroyed!");
            StopCoroutine(DamageBlink(3));
            DeathBlink();

            canShoot = false;
            canTakeDmg = false;
            isAlive = false;
            for (int i = 0; i < numOfBolts; i++)
            {
                //GameManager.GM.boltsOnField[otherTeam]++;

                GameObject b = Instantiate(boltToDrop, transform.position, Quaternion.identity);
                b.tag = "Bolt" + otherTeam;
                b.GetComponent<Rigidbody>().AddForce(Vector3.up * 40f);
                b.GetComponent<BoltEffects>().SetMyVars(otherTeam);
                b.GetComponent<BoltEffects>().Invoke("DestroyMe", 20);//make it destroy itself and decrement after a while.

                //Destroy(b, 20f);
            }
            StatsManager.SM.towerKills[towerNum]++;
            UIManager.UIM.teamTowerKills[towerNum].text = StatsManager.SM.towerKills[towerNum].ToString();

            if(uiInfo!=null)//it's null for the Cores.
            uiInfo.TurnOffImage();
            GameManager.GM.PlayClip(12);
            //remove yourself from the ObjectManager's List:
            ObjectsManager.instance.allTowers.Remove(this);
            StartCoroutine(DieDieDie());
            //Destroy(gameObject);

        }
    }

    [Button]
    public void CallDie(){
        StartCoroutine(DieDieDie());

    }
    public IEnumerator DieDieDie()
    {
        GameObject g = Instantiate(deathParticles, myTop.position, Quaternion.identity);
        yield return new WaitForSeconds(deathDelay);
        //turn stop action on for particles so they die out when they're done
        //(instead of dying suddenly from destroy()
        foreach(ParticleSystem ps in g.GetComponentsInChildren<ParticleSystem>())
        {
            var sa = ps.main;
            sa.stopAction = ParticleSystemStopAction.Destroy;
            sa.loop = false;
            ps.gameObject.transform.SetParent(null); //decouple from parent shell
        }
        Destroy(g); //destroy shell
        Destroy(gameObject);
    }

    public IEnumerator DamageBlink(int howManyTimes)
    {
        int i = howManyTimes;
        while (i > 0)
        {
            statusImage.sprite = hurtSprite;
            statusImage.enabled = true;
            yield return new WaitForSeconds(.4f);
            statusImage.enabled = false;
            i--;

        }
    }
    public void DeathBlink()
    {
        //this is spawning a new canvas that's a duplicate of the canvas before
        //the tower destroys itself.
        //not sure this is the best way.
        Canvas ghostCan = GetComponentInChildren<Canvas>();
        Quaternion rot = ghostCan.transform.localRotation;
        //GameObject gc = Instantiate(ghostCan.gameObject, transform.position, Quaternion.Euler(new Vector3(-90, 180, 0)));
        GameObject gc = Instantiate(ghostCan.gameObject, transform.position, rot);
        foreach (Image i in gc.GetComponentsInChildren<Image>())
        {
            if (i.name == "Image") //eh, never like doing this, but here we go.
            {
                i.sprite = deadSprite;
            }
        }
    }
    

    public void Shoot()
    {
        //if (attacker == null)
        //{
        //    if (towerNum == 1)
        //        SearchForAttacker(ObjectsManager.instance.enemyList2);
        //    else if (towerNum == 2)
        //        SearchForAttacker(ObjectsManager.instance.enemyList1);
        //}

        //if (IsWithinDistance())
        //{
        //    attacker.GetComponent<Enemy>().TakeDamage(dmgToGive);

        //    StartCoroutine(ShootDelay());
        //}

        if (attacker != null)
        {
            attacker.GetComponent<Enemy>().TakeDamage(dmgToGive, towerNum, true);
            SendDamageOBJ(attacker.transform);

            Debug.DrawLine(transform.position, attacker.transform.position, Color.red, .3f);

            StartCoroutine(ShootDelay());
        }
    }

    public void SendDamageOBJ(Transform destination)
    {
        GameObject g = Instantiate(projectilePrefab, myTop.position, Quaternion.identity);
        //!should change particle color to be = to tower color.
        g.transform.SetParent(destination); //the script on this object will make it go to its parent then die
    }

    public void SearchForAttacker(List<GameObject> enemies)
    {
        if (enemies.Count == 0)
            return;

        // Loop through list of enemies and assign which enemy the tower should attack
        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject tempAttacker = enemies[i];
            float dist = Vector3.Distance(transform.position, tempAttacker.transform.position);

            if (dist <= maxShootDist)
                attacker = tempAttacker;
        }
    }

    public void ChargeUp()
    {
        beingCharged = true;
        diminCD = diminCDOG;
        //you can charge up the first tower in your lane.
        //if it's charged, further charging it will charge the next tower in that lane.
        if (!isSupercharged)
        {
            sc += Time.deltaTime * scMult;
            if (sc >= scOG)
            {
                isSupercharged = true;
                chargeParts.Play();
            }
        }
        else
        {
            if (nextTower != false)
            {
                nextTower.ChargeUp();
            }
        }
    }

    void DiminishCharge()
    {
        if (sc > 0)
        {
            if (isSupercharged)
            {
                //it goes down faster if supercharged.
                sc -= Time.deltaTime * scMult;
            }
            else
            {
                sc -= Time.deltaTime;
            }
        }
        else
        {
            if (isSupercharged)
            {
                if (nextTower!=null && nextTower.isSupercharged)
                {
                    nextTower.ChargeDown();
                }
                ChargeDown();

            }
        }
    }

    public void ChargeDown()
    {
        isSupercharged = false;
        sc = 0;
        //also make a cool noise

        //and show the player
        chargeParts.Stop();

    }

    IEnumerator ShootDelay()
    {
        canShoot = false;

        yield return new WaitForSeconds(shootDelay);

        if (health > 0)
            canShoot = true;
    }

    public bool IsWithinDistance()
    {
        if (attacker != null)
        {
            if (Vector3.Distance(transform.position, attacker.transform.position) <= maxShootDist)
                return true;
        }

        return false;
    }

    public void DrawLine()
    {
        if (IsWithinDistance() && isAlive)
        {
            lineRend.enabled = true;

            Vector3 from = transform.GetChild(1).transform.position;
            Vector3 to = attacker.transform.position;

            lineRend.SetPosition(0, from);
            lineRend.SetPosition(1, to);
        }
        else
        {
            lineRend.enabled = false;
        }
    }

    public void Freeze(bool trueForOn)
    {
        isFrozen = trueForOn;
        iceCube.SetActive(trueForOn);
    }

    public void SetMaxHealth(int amnt)
    {
        mxHealth = amnt;
        health = mxHealth;
        UIManager.UIM.SetTexts();

        //gotta update UI if core health is changed??
        //UIManager.UIM.coreHealthText[
    }

    [Button]
    public void TakeTenPercent()
    {
        TakeDamage(mxHealth*.1f);
    }

}
