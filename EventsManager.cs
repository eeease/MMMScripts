using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public class EventsManager : MonoBehaviour
{
    public static EventsManager EM;
    public EventCard overrideEvent;
    public List<EventCard> allEvents;
    public string[] randomLanes;


    public GameObject eventCardsUI;

    public float animTotalTime;
    Animator anim;

    public List<EventCard> curseCards, bonusCards, miscCards;

    public EventCard chosenEvent;

    bool firstChoice = true;
    int index;
    int[] typeIndex = new int[3];
    public GameObject[] laneLavas;
    public GameObject laneSmoke;
    List<ParticleSystem> laneSmokeList = new List<ParticleSystem>();
    public float lavaDmg = 4f;
    public float turretDmg = 0.05f;
    public int researchRate = 1;
    public int[] targetsBroken;
    public int numOfTargetsOn = 10;
    public List<GameObject> activatedTargets = new List<GameObject>();

    [Header("Event Card Parts")]
    public Image icon;
    public Image main, helper;
    public Text cName, effect, countDown;
    public float eventTimerForText;
    public bool countingDown = false;


    [Header("Event Bools")]
    public bool lazarDamages;
    public bool blindBoard, bigGameHunter, infiniteSwap, breakingTargets;
    public int doubleMatchNum = 0; //which match amount will produce double? 0 if none
    public int lavaLane = 999;
    private delegate void Callback();
    Toggle refEventTog; //this is going to get stored when the player selects a toggle, that way i can save pp more easily...?

    // Start is called before the first frame update
    void Start()
    {
        EM = this;

        anim = eventCardsUI.GetComponent<Animator>();
        UpdateAnimClipTimes();
        ClearAllEventLists();
        CheckAddAllEvents();
        Shuffle(curseCards);
        Shuffle(bonusCards);
        Shuffle(miscCards);

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.RightBracket)){
            //foreach(Board b in ObjectsManager.instance.boardsInplay)
            //{
            //    b.SpawnMega(1, 10);
            //}
        }
        if(countingDown && eventTimerForText>1)
        {
            eventTimerForText -= Time.deltaTime;
            //countDown.text = StatsManager.SM.FormatTime(eventTimerForText);
        }

        if (breakingTargets && activatedTargets.Count == 0)
        {
            StopCoroutine(DisableTargets(GameManager.GM.eventDuration));
            StartCoroutine(DisableTargets(0));
        }
    }

    //going to use strings so i can just shuffle the lists of events first then pick them in order.
    //which is an easy way of not repeating them.
    public void ActivateCurse(string which)
    {
        print("Activating Curse: " + which);
        chosenEvent = curseCards[FindString(which, 0)];

        switch (which)
        {
            case "LaneFreeze":
                //should we first unfreeze previously frozen towers?:
                foreach(Tower t in ObjectsManager.instance.allTowers)
                {
                    if (t.isFrozen)
                    {
                        t.Freeze(false);
                    }
                }

                //pick a random lane:
                string l = randomLanes[RandomLane()];

                //then go through all towers with that name and freeze 'em:
                foreach(Tower t in ObjectsManager.instance.allTowers)
                {
                    if (t.myLane == l)
                    {
                        t.Freeze(true);
                    }
                }
                chosenEvent.effect = l + " lane turrets disabled!  Shoot the ice!";

                //StartCoroutine(TurnOffBool(() => lazarDamages = false, 10f));

                break;

            case "LaneTurrets":
                //turn off other turrets if we've cycled around and there were some already on:
                foreach(TurretHelper th in FindObjectsOfType<TurretHelper>())
                {
                    th.gameObject.SetActive(false);
                }
                int rl = RandomLane();
                string rlst = randomLanes[rl];
                ObjectsManager.instance.laneTurrets[rl].SetActive(true);
                chosenEvent.effect = rlst + " lane turrets! Disable them!";

                break;
        }
    }
    public void ActivateBonus(string which)
    {
        print("Activating Bonus: " + which);
        chosenEvent = bonusCards[FindString(which, 1)];

        switch (which)
        {
            case "FaceMelter":
                lazarDamages = true;
                StartCoroutine(TurnOffBool(() => lazarDamages = false, GameManager.GM.eventDuration+animTotalTime));
                break;

            case "BGH":
                bigGameHunter = true;
                StartCoroutine(TurnOffBool(() => bigGameHunter = false, GameManager.GM.eventDuration + animTotalTime));
                break;

            case "Twinsanity":
                doubleMatchNum = Random.Range(4, 7);
                chosenEvent.effect = "Match-" + doubleMatchNum + "s spawn double minions!";
                StartCoroutine(TurnOffBool(() => doubleMatchNum = 0, GameManager.GM.eventDuration));
                break;

            case "ResearchBonus":
                researchRate = 2;
                StartCoroutine(TurnOffBool(() => researchRate = 1, GameManager.GM.eventDuration));
                break;

            case "SwapQueen":
                infiniteSwap = true;
                StartCoroutine(TurnOffBool(() => infiniteSwap = false, GameManager.GM.eventDuration + animTotalTime));

                break;

        }
    }
    public void ActivateMisc(string which)
    {
        print("Activating Misc: " + which);
        chosenEvent = miscCards[FindString(which, 2)];

        switch (which)
        {
            case "BlindBoard":
                BlindBoards(true);
                StartCoroutine(TurnOffBool(() => BlindBoards(false), GameManager.GM.eventDuration));

                break;

            case "BreakTheTargets":
                breakingTargets = true;
                //shuffle the list of targets, then turn on the first ten:
                Shuffle(ObjectsManager.instance.allTargets);
                activatedTargets.Clear();
                //copy them to a new list so they're more manageable...?
                for(int i=0; i<numOfTargetsOn; i++)
                {
                    activatedTargets.Add(ObjectsManager.instance.allTargets[i]);
                    activatedTargets[i].SetActive(true);
                }
                StartCoroutine(DisableTargets(GameManager.GM.eventDuration));
                break;

            case "LaneLava":
                int r = RandomLane();//pick it once.
                lavaLane = r;
                //turn it on:
                laneLavas[r].SetActive(true);
                string l = randomLanes[r];
                chosenEvent.effect = l + " lane is lava! Hot hot hot!";

                StartCoroutine(TurnOffBool(() => laneLavas[r].SetActive(false), GameManager.GM.eventDuration));
                StartCoroutine(TurnOffBool(() => lavaLane=999, GameManager.GM.eventDuration));

                //tell all enemies in these lanes to start taking damage:
                foreach(Enemy e in FindObjectsOfType<Enemy>()){
                    if(e.laneNum==r && !e.isInLava)
                    {
                        e.InvokeRepeating("TakeLavaDamage", 1f, 1f);
                    }
                }

                break;

            case "LaneSmoke":
                laneSmoke.SetActive(true);
                foreach(ParticleSystem ps in laneSmoke.GetComponentsInChildren<ParticleSystem>())
                {
                    if (!laneSmokeList.Contains(ps))
                    {
                        laneSmokeList.Add(ps);
                    }
                }
                foreach(ParticleSystem p in laneSmokeList)
                {
                    p.loop = true;
                    p.Play();
                }

                StartCoroutine(DisableSmoke(laneSmokeList, GameManager.GM.eventDuration-10f));

                StartCoroutine(TurnOffBool(() => laneSmoke.SetActive(false), GameManager.GM.eventDuration+5f)); //delayed to allow the loop turn off to stop it 'naturally'
                break;
        }
    }

    [Button]
    public void ActivateOverride()
    {
        string w = overrideEvent.name;
        switch (overrideEvent.myType)
        {
            case EventCard.eType.Curse:
                ActivateCurse(w);
                break;

            case EventCard.eType.Bonus:
                ActivateBonus(w);
                break;

            case EventCard.eType.Mis:
                ActivateMisc(w);
                break;
        }
    }
    
    public void ChooseACard()
    {
        if (!GameManager.GM.gameIsEnded)
        {


            if(curseCards.Count==0 && bonusCards.Count ==0 && miscCards.Count==0)
            {
                return;
            }
            //explanation: The first event that happens is random.
            //Then, it follows in order: Curse->Bonus->Misc
            if (firstChoice)
            {
                index = Random.Range(0, 3);
                firstChoice = false;
            }
            else
            {
                //!hardcoded and bleh:
                if (index == 2)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
            }

            //ahhhh this seems to work!
            bool foundAnEvent = false;
            while(!foundAnEvent)
            {
                //with the option to turn off events, will have to check
                //if the event list has anything in it before activating the card.
                switch (index)
                {
                    case (0):
                        if (curseCards.Count <= 0)
                        {
                            index++;
                        }else{
                            foundAnEvent = true;
                        }
                        break;

                    case (1):
                        if (bonusCards.Count <= 0)
                        {
                            index++;
                        }
                        else
                        {
                            foundAnEvent = true;
                        }
                        break;

                    case (2):
                        if (miscCards.Count <= 0)
                        {
                            index = 0;
                        }
                        else
                        {
                            foundAnEvent = true;
                        }
                        break;
                }

            }

            switch (index)
            {
                case (0):
                    //pick a curse:
                    ActivateCurse(curseCards[typeIndex[0]].name);

                    IncrementListIndex(curseCards);
                    break;

                case (1):
                    //pick a bonus:
                    ActivateBonus(bonusCards[typeIndex[1]].name);

                    IncrementListIndex(bonusCards);
                    break;

                case (2):
                    //pick a misc:
                    ActivateMisc(miscCards[typeIndex[2]].name);

                    IncrementListIndex(miscCards);
                    break;
            }

            StartCoroutine(PauseForEvent(animTotalTime));
            AssignCard();
        }
    }

    void IncrementListIndex(List<EventCard> whaaat){
        //increment the type index unless it has reached the max:

        if (typeIndex[index] < whaaat.Count-1)
        {
            typeIndex[index]++;
        }else{
            typeIndex[index] = 0;
        }
    }

    public void UpdateAnimClipTimes()
    {
        animTotalTime = 0;
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            animTotalTime += clip.length;

            //switch (clip.name)
            //{
            //    case "EventDealOut":
            //        animTotalTime += clip.length;
            //        break;
            //    case "EventReveal":
            //        animTotalTime += clip.length;
            //        break;
            //}
        }
    }

    public IEnumerator PauseForEvent(float delay)
    {

        StartCoroutine(StartCountDown());
        GameManager.GM.everythingPause = true;
        eventCardsUI.SetActive(true);


        yield return new WaitForSeconds(delay / 2);
        //play a sound clip for some events?
        PlayEventClip();

        yield return new WaitForSeconds(delay + 1f);
        eventCardsUI.SetActive(false);
        GameManager.GM.everythingPause = false;



    }
    public IEnumerator StartCountDown()
    {
        eventTimerForText = GameManager.GM.eventDuration;
        countingDown = true;
        yield return new WaitForSeconds(GameManager.GM.eventDuration);
        countingDown = false;
    }

    void PlayEventClip()
    {
        switch (chosenEvent.name)
        {
            case "LaneFreeze":
                GameManager.GM.PlayClip(15);
                break;
        }
    }

    public void AssignCard(){
        icon.sprite = chosenEvent.icon;
        main.sprite = chosenEvent.mainImage;
        helper.sprite = chosenEvent.helperimage;
        cName.text = chosenEvent.eventName;
        effect.text = chosenEvent.effect;


        foreach (PlayerShoot ps in MultiplayerWithBindingsExample.PlayerManager.PM.players)
        {
            ps.AssignMiniCard(icon.sprite, main.sprite, helper.sprite, cName.text, effect.text);
        }
    }
    public int RandomLane()
    {
        return Random.Range(0, 3);
    }

    //going to call from an array of strings instead
    //public string RandomLaneString()
    //{
    //    int r = Random.Range(0, 3);
    //    string lane = string.Empty;
    //    switch (r)
    //    {
    //        case 0:
    //            lane = "Top";
    //            break;
    //        case 1:
    //            lane = "Mid";
    //            break;
    //        case 2:
    //            lane = "Bottom";
    //            break;
    //    }
    //    return lane;
    //}
    public void Shuffle(IList ts)
    {
        int count = ts.Count;
        int last = count - 1;
        for (int i = 0; i < last; i++)
        {
            int r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    IEnumerator TurnOffBool(Callback callback, float delay)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();

        ResetMiniCards();

    }

    IEnumerator DisableSmoke(List<ParticleSystem> parts, float del)
    {
        yield return new WaitForSeconds(del);
        foreach(ParticleSystem ps in parts)
        {
            ps.loop = false;
        }
        ResetMiniCards();

    }

    IEnumerator DisableTargets(float del)
    {
        yield return new WaitForSeconds(del);
        for(int i=0; i<ObjectsManager.instance.boardsInplay.Count; i++)
        {
            //spawns down mid atm:
            ObjectsManager.instance.boardsInplay[i].SpawnMega(1, targetsBroken[i]);

            //clear targets count:
            targetsBroken[i] = 0;
        }

        //turn off and remove all activated:
        if (activatedTargets.Count > 0)
        {
            for(int i = activatedTargets.Count-1; i>0; i--)
            {
                activatedTargets[i].SetActive(false);
                activatedTargets.RemoveAt(i);
            }
            //foreach (GameObject go in activatedTargets)
            //{
            //    go.SetActive(false);
            //    activatedTargets.Remove(go);
                
            //}

        }
        breakingTargets = false;
        
        ResetMiniCards();

    }

    public void ResetMiniCards()
    {

        //reset players' mini cards:
        foreach (PlayerShoot ps in MultiplayerWithBindingsExample.PlayerManager.PM.players)
        {
            ps.ResetEventInfo();
        }
    }

    /// <summary>
    /// 0=curse; 1=bonus; 2=purge
    /// </summary>
    /// <param name="cardName"></param>
    /// <param name="whichList"></param>
    /// <returns></returns>
    public int FindString(string cardName, int whichList)
    {
        int temp = 0;
        switch (whichList)
        {
            case 0:
                for (int i = 0; i < curseCards.Count; i++)
                {
                    if (curseCards[i].name == cardName)
                    {
                        temp = i;
                        break;
                    }
                }
                break;

            case 1:
                for (int i = 0; i < bonusCards.Count; i++)
                {
                    if (bonusCards[i].name == cardName)
                    {
                        temp = i;
                        break;
                    }
                }
                break;

            case 2:
                for (int i = 0; i < miscCards.Count; i++)
                {
                    if (miscCards[i].name == cardName)
                    {
                        temp = i;
                        break;
                    }
                }
                break;
        }


        return temp;
    }

    public void BlindBoards(bool beBlinded)
    {
        blindBoard = beBlinded;

        foreach (Board b in ObjectsManager.instance.boardsInplay)
        {
            b.BlindAllGems(beBlinded);
        }
    }

    public void ToggSel(Toggle t)
    {
        refEventTog = t;
    }

    public void ClearAllEventLists()
    {
        curseCards.Clear();
        bonusCards.Clear();
        miscCards.Clear();
    }

    public void CheckAddAllEvents()
    {
        for (int i = 0; i < allEvents.Count; i++)
        {
            AddEvent(allEvents[i]);
        }
        //foreach(EventCard e in allEvents)
        //{
        //    AddEvent(e);
        //}
    }

    public void AddEvent(EventCard which){

        if(PlayerPrefsX.GetBool(which.name))
        {
            switch (which.myType)
            {
                case EventCard.eType.Curse:
                    if (!curseCards.Contains(which))
                    {
                        curseCards.Add(which);
                    }

                    break;

                case EventCard.eType.Bonus:
                    if (!bonusCards.Contains(which))
                    {
                        bonusCards.Add(which);
                    }

                    break;

                case EventCard.eType.Mis:
                    if (!miscCards.Contains(which))
                    {
                        miscCards.Add(which);
                    }
                    break;
            }

        }
    }

    //probably not totally extendable (the toggle should send in a bool?) but should work for now
    //also, for persistence, should keep track of what players want on or off.
    public void AddRemoveEvent(EventCard which)
    {
        //save this option:
        if (refEventTog != null)
        PlayerPrefsX.SetBool(which.name, refEventTog.isOn); //also need to take in toggle ;\\\
        //print("setting " + which.name + " " + refEventTog.isOn);
        switch (which.myType)
        {
            case EventCard.eType.Curse:
                if (curseCards.Contains(which))
                {
                    curseCards.Remove(which);
                }
                else
                {
                    if(PlayerPrefsX.GetBool(which.name))
                    curseCards.Add(which);
                }
                break;

            case EventCard.eType.Bonus:
                if (bonusCards.Contains(which))
                {
                    bonusCards.Remove(which);
                }
                else
                {
                    if (PlayerPrefsX.GetBool(which.name))
                    bonusCards.Add(which);
                }
                break;

            case EventCard.eType.Mis:
                if (miscCards.Contains(which))
                {
                    miscCards.Remove(which);
                }
                else
                {
                    if (PlayerPrefsX.GetBool(which.name))
                    miscCards.Add(which);
                }
                break;
        }
    }
}
