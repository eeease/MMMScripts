using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour
{
    public float health = 10;
    private float maxHealth;
    public MeshRenderer applyColorHere;
    private Color[] matColors;
    public int column; //going to set these in board spawner cause we need to use a spacer multiplier
    public int row;
    public int prevColumn, prevRow; //in case we want to deal with swapping the gems back after moving them, ex.
    //public int targetX;
    //public int targetY;
    public bool isMatched = false;
    public Board brd;
    private GameObject otherDot;
    //not sure if we're using "swipe" controls:
    private Vector2 firstTouchPos;
    private Vector2 finalTouchPos;
    private Vector2 tempPosition;
    public float swipeAngle = 0;
    public float swipeResist = 1f; //actually move this amount to calculate a swipe (can't do a pixel move).
    public FindMatches matchMaker;
    [Header("Delays")]
    public float checkMatchDel;
    public float fallSpeed;
  


    //my vars:
    public int myColor;
    string myColorName;
    public GameObject deathParticles, puffParts, dmgParts;
    bool shotDead; //for gems that were shot destroyed, not match3'd
    public Vector2 targetPos;
    public bool canTakeDamage = false; //this will turn true when it gets to its position.
    PlayerShoot playerHittingMe;
    public float swapRefillAmount = 10f;
    AudioSource myAudio;
    public AudioClip[] sfx;
    public GameObject questionCan;
    public bool blinded;
    public Image buffImg;
    public Canvas dmgCan;
    public Sprite[] cracks;
    public Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        maxHealth = health;
        myAudio = GetComponent<AudioSource>();

        if(GameManager.GM.challengeMaking){
            //ChangeColor();
        }
        matColors = new Color[applyColorHere.materials.Length];
        for(int i = 0; i < matColors.Length; i++)
        {
            matColors[i] = applyColorHere.materials[i].color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //targetX = column;
        //targetY = row;
        //FindMatches();

        //should modify this so it's not always being called by every gem.
        //also, when swapped, gems don't wait to get to their position before matching, which could look a bit confusing.
        MoveToYourSpot();
        
   
    }

    public void MoveToYourSpot()
    {
        if (Vector2.Distance(transform.localPosition, targetPos) > .2f)
        {
            transform.localPosition = Vector2.MoveTowards(transform.localPosition, targetPos, fallSpeed * Time.deltaTime);

        }
        else
        {
            transform.localPosition = targetPos;

        }
    }
    public void SetColor(int color)
    {
        myColor = color;
        myColorName = GameManager.GM.possibleColors[color].name;

        if (!EventsManager.EM.blindBoard){
            applyColorHere.materials[2].color = GameManager.GM.actualColors[color];

        }else{
            RiddleMe(true);
            blinded = true;
            //GetComponentInChildren<MeshRenderer>().material = GameManager.GM.blindColor;
            //questionCan.SetActive(true);

        }

    }

    public void SetCoordinates(int x, int y)
    {
        //targetX = x;
        //targetY = y;
        row = y;
        column = x;
        prevRow = row;
        prevColumn = column;
    }

    public void MoveYourself(Vector2 tPos)
    {
        //transform.localPosition = tPos;
        targetPos = tPos;
        GameManager.GM.PlayClip(sfx[3], .1f); //low because i think both gems are going to play this
    }

    public void SetFirstTouch(Vector3 startPos)
    {
        firstTouchPos = startPos;
        //print(firstTouchPos);
    }

    public void SetLastTouch(Vector3 endPos)
    {
        finalTouchPos = endPos;
        CalculateAngle();
    }

    void CalculateAngle()
    {
        if(Mathf.Abs(finalTouchPos.y-firstTouchPos.y) > swipeResist || Mathf.Abs(finalTouchPos.x - firstTouchPos.x) > swipeResist)
        {
            swipeAngle = Mathf.Atan2(finalTouchPos.y - firstTouchPos.y, finalTouchPos.x - firstTouchPos.x) * 180 / Mathf.PI;
            MovePieces();
        }
    }

    void MovePieces()
    {
        //may not use this just yet
        if((swipeAngle >-45 && swipeAngle <= 45) && column<brd.width-1)
        {
            //right swipe:
            otherDot = brd.allDots[column + 1, row];
            otherDot.GetComponent<Gem>().column -= 1;
            column += 1;
        }else if ((swipeAngle > 45 && swipeAngle <= 135) && row < brd.height-1)
        {
            //up swipe:
            otherDot = brd.allDots[column, row+1];
            otherDot.GetComponent<Gem>().row -= 1;
            row += 1;
        }
        else if((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {
            //left swipe:
            otherDot = brd.allDots[column - 1, row];
            otherDot.GetComponent<Gem>().column += 1;
            column -= 1;
        }else if ((swipeAngle < -45 && swipeAngle >= -135) && row>0)
        {
            //down swipe:
            otherDot = brd.allDots[column, row-1];
            otherDot.GetComponent<Gem>().row += 1;
            row -= 1;
        }
        //StartCoroutine(CheckMoveCo()); //to swap them back if they're not a match.
    }

    public IEnumerator CheckMoveCo()
    {
        yield return new WaitForSeconds(checkMatchDel);
        if (otherDot != null)
        {
            if(!isMatched && !otherDot.GetComponent<Gem>().isMatched)
            {
                otherDot.GetComponent<Gem>().row = row;
                otherDot.GetComponent<Gem>().column = column;
                row = prevRow;
                column = prevColumn;
            }
            else
            {
                brd.DestroyMatches();
            }
            otherDot = null;
        }
        
    }

    /*
    void FindMatches()
    {
        if (column > 0 && column < brd.width-1)
        {
            GameObject lDot1 = brd.allDots[column - 1, row];
            GameObject rDot1 = brd.allDots[column + 1, row];
            if (lDot1 && rDot1)
            {
                if (lDot1.GetComponent<Gem>().myColor == myColor && rDot1.GetComponent<Gem>().myColor == myColor)
                {
                    lDot1.GetComponent<Gem>().isMatched = true;
                    rDot1.GetComponent<Gem>().isMatched = true;
                    isMatched = true;
                    brd.MakingMatchOf(myColorName);
                    brd.DestroyMatches();

                }

            }

        }

        //check up and down, too.
        if (row > 0 && row < brd.height-1)
        {
            GameObject uDot1 = brd.allDots[column, row+1];
            GameObject dDot1 = brd.allDots[column, row-1];
            if (uDot1 && dDot1)
            {
                if (uDot1.GetComponent<Gem>().myColor == myColor && dDot1.GetComponent<Gem>().myColor == myColor)
                {
                    uDot1.GetComponent<Gem>().isMatched = true;
                    dDot1.GetComponent<Gem>().isMatched = true;
                    isMatched = true;
                    brd.MakingMatchOf(myColorName);
                    brd.DestroyMatches();

                }

            }
        }
    }
    */

    public void DestroyMe()
    {
        //print(name + "play anim then die");
        anim.enabled = true;
        //drop particles, shrink, etc etc.:
        //if (!shotDead)
        //{
        //    //more fanfare for being matched:
        //    GameObject p = Instantiate(deathParticles, transform.position, Quaternion.identity);
        //    p.GetComponent<ParticleSystem>().startColor = GameManager.GM.actualColors[myColor];
        //    GameManager.GM.PlayClip(sfx[2], .4f);
        //}
        //else
        //{
        //    //less fanfare for being shot
        //    Instantiate(puffParts, transform.position, Quaternion.identity);
        //    GameManager.GM.PlayClip(sfx[1], .4f);

        //}

        //Destroy(gameObject, GameManager.GM.GetClipTime(anim, "GemDestroy"));
        StartCoroutine(DeathAfterAnim(GameManager.GM.GetClipTime(anim, "GemDestroy")));
    }

    public IEnumerator DeathAfterAnim(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!shotDead)
        {
            //more fanfare for being matched:
            GameObject p = Instantiate(deathParticles, transform.position, Quaternion.identity);
            p.GetComponent<ParticleSystem>().startColor = GameManager.GM.actualColors[myColor];
            GameManager.GM.PlayClip(sfx[2], .4f);
        }
        else
        {
            //less fanfare for being shot
            Instantiate(puffParts, transform.position, Quaternion.identity);
            GameManager.GM.PlayClip(sfx[1], .4f);

        }

        Destroy(gameObject);
    }

    public void TakeDamage(float howMuch, PlayerShoot p)
    {
        if (brd.currentState == GameState.move)
        {
            if (playerHittingMe !=p)
            {
                playerHittingMe = p;
                //also, tell the big board who the player is that's shooting this board:
                //if (brd.myPlayer == null)
                //{
                //    brd.myPlayer = p;
                //    brd.teamNum = p.teamNum; //this is bad and it's probably what caused the minion auto kill bug.
                //}
            }
            health -= howMuch;
            GameObject parts = Instantiate(dmgParts, transform.position, Quaternion.identity);
            parts.GetComponent<ParticleSystem>().startColor = GameManager.GM.actualColors[myColor];
            if (!myAudio.isPlaying)
            {
                myAudio.clip = sfx[0];
                myAudio.volume = GameManager.GM.SFXVol();
                myAudio.Play();

            }

            if (health <= 0)
            {
                isMatched = true; //not sure if this is the best way to deal with this atm.
                shotDead = true;
                //inc GemFrenzy if you're playing that mode.
                if (GameManager.GM.gameMode == 1)
                    brd.IncGemsDestroyedAndCheck(myColor, 1);

                if (playerHittingMe!=null)
                {
                   

                    StatsManager.SM.IncrementStat(p.playerNum, 6);

                    //StatsManager.SM.pStats[p.playerNum, 6]++;

                    playerHittingMe.RefillSwapJuice(swapRefillAmount);
                    if (playerHittingMe.rumbleOn)
                    {
                        MultiplayerWithBindingsExample.PlayerManager.PM.VibrateDef(playerHittingMe.Device);
                    }
                }
                else
                {
                    brd.refillDelay = brd.refillDelayAI;
                    StatsManager.SM.IncrementStat(1, 6);
                }
                //if it's autoplay, then wait until the last one is destroyed?
                //if (!brd.autoPlay || brd.doneAutoDest)
                //{
                    brd.currentState = GameState.wait;
                    brd.waitWall.SetActive(true);
                    brd.DestroyMatches();
                    matchMaker.FindAllMatches();
                //    brd.doneAutoDest = false;
                //}
             
            }
            UpdateDamageCan();

        }

    }

    public void RiddleMe(bool trueForOn)
    {
        questionCan.SetActive(trueForOn);
        buffImg.enabled = !trueForOn;
        if (trueForOn)
        {
            applyColorHere.materials[2].color = Color.white;
        }
        else
        {
            applyColorHere.materials[2].color = GameManager.GM.actualColors[myColor];
            matColors[2] = GameManager.GM.actualColors[myColor];
        }
    }

    public void CheckMyBuffHints()
    {
        
        //also turn off the image if the player turned if off in their options:
        if (brd.myPlayer != null)
        {
            if(!blinded)
            buffImg.enabled = brd.myPlayer.gemBuffHints;
        }
        //this needs to be fixed up:
        if (GameManager.GM.minionDiff && !GameManager.GM.challengeMaking)
        {
            buffImg.sprite = UIManager.UIM.buffIcons[myColor];
        }
        else
        {
            buffImg.enabled = false;
        }
    }
    public void RevertMyColors()
    {
        for (int i = 0; i < matColors.Length; i++)
        {
            applyColorHere.materials[i].color = matColors[i];
        }
    }

    void UpdateDamageCan()
    {

        if (!isMatched)
        {
            dmgCan.gameObject.SetActive(true);
            Image dI = dmgCan.GetComponentInChildren<Image>();
            if (health <= maxHealth / 3f)
            {
                dI.sprite = cracks[2];
            }
            else if (health <= (maxHealth * 0.66f))
            {
                dI.sprite = cracks[1];

            }
            else if (health <= maxHealth)
            {
                dI.sprite = cracks[0];

            }

        }else{
            dmgCan.gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if(GameManager.GM.challengeMaking)
        {
            if(GameManager.GM.painting){
                ChangeColor();

            }else if (GameManager.GM.destroying)
            {
                TakeDamage(9999, null);
            }

        }
    }

    public void ChangeColor()
    {

        if(myColor>=GameManager.GM.actualColors.Length-1)
        {
            myColor = 0;
        }else{
            myColor++;
        }
        SetColor(myColor);
        int mathIsGreat = (column * brd.width) + row;
        GameManager.GM.challengeBoard[mathIsGreat] = myColor;

    }
}
