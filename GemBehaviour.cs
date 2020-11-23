using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemBehaviour : MonoBehaviour
{
    public MeshRenderer applyColorHere;
    public float startTime;
    public float health;
    //public Material[] possibleColors;
    public int myColor;
    public string myColorName; //because i'm sick of checking it from GM
    public float downVel;
    public GemSpawner papaSpawner; //easier to keep track of it this way
    public float rLength;
    public int xCoord, yCoord;

    public bool markForRemoval;
    public bool isMatched = false;
    private void Awake()
    {
        startTime = Time.timeSinceLevelLoad;
    }
    // Start is called before the first frame update
    void Start()
    {
        //moved color setting to GemSpawner so all boards can spawn the same.

        //myColor = Random.Range(0, possibleColors.Length);
        //GetComponentInChildren<MeshRenderer>().material = possibleColors[myColor];
        GetComponent<Rigidbody>().velocity = Vector3.down * downVel;
        //papaSpawner = transform.parent.GetComponentInParent<GemSpawner>();
    }

    public void SetColor (int color)
    {
        myColor = color;
        applyColorHere.materials[2] = GameManager.GM.possibleColors[color];
        myColorName = GameManager.GM.possibleColors[color].name;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            FindMatches();
        }
    }

    void ShootRays()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, transform.parent.TransformDirection(Vector3.left), out hit, rLength);
        Debug.DrawRay(transform.position, transform.parent.TransformDirection(Vector3.left) * rLength, Color.green);
        Physics.Raycast(transform.position, transform.parent.TransformDirection(Vector3.right), out hit, rLength);
        Debug.DrawRay(transform.position, transform.parent.TransformDirection(Vector3.right) * rLength, Color.green);
        Physics.Raycast(transform.position, transform.parent.TransformDirection(Vector3.up), out hit, rLength);
        Debug.DrawRay(transform.position, transform.parent.TransformDirection(Vector3.up) * rLength, Color.green);
        Physics.Raycast(transform.position, transform.parent.TransformDirection(Vector3.down), out hit, rLength);
        Debug.DrawRay(transform.position, transform.parent.TransformDirection(Vector3.down) * rLength, Color.green);
    }
    void OnMouseDown()
    {
        //DestroyMe();
        //Destroy(gameObject);
    }

    public void DestroyMe()
    {
        papaSpawner.RemoveGem(xCoord, yCoord);

        //StartCoroutine(papaSpawner.SpawnGemInColumn(papaSpawner.columns.IndexOf(transform.parent)));
        //papaSpawner.Placeholder(papaSpawner.columns.IndexOf(transform.parent));

        Destroy(gameObject);
    }

    public void TakeDamage(float dmg)
    {
        if (Mathf.Abs(GetComponent<Rigidbody>().velocity.y) < 0.1f)
        {
            health -= dmg;
            if (dmg > 0)
                GetComponentInChildren<Transform>().Rotate(Vector3.up, 500f);
            //GetComponentInChildren<Transform>().Rotate(Vector3.left, 500f);
            if (health <= 0)
            {
                DestroyMe();
            }

        }
    }

    public void CheckForMatch()
    {
        GemBehaviour uGem = null;
        GemBehaviour dGem = null;
        GemBehaviour lGem = null;
        GemBehaviour rGem = null;
        //find all your surrounding gems:
        if (yCoord < papaSpawner.rows - 1)
        {
            //print("my y = " + yCoord);
            uGem = papaSpawner.gemsInMe[xCoord, yCoord + 1].GetComponent<GemBehaviour>();
        }
        if (yCoord > 0)
        {
            dGem = papaSpawner.gemsInMe[xCoord, yCoord - 1].GetComponent<GemBehaviour>();
        }
        if (xCoord > 0)
        {
            lGem = papaSpawner.gemsInMe[xCoord - 1, yCoord].GetComponent<GemBehaviour>();
        }
        if (xCoord < papaSpawner.cols - 1)
        {
            rGem = papaSpawner.gemsInMe[xCoord + 1, yCoord].GetComponent<GemBehaviour>();
        }


        //check left:
        //if (lGem)
        //{
        //    if (lGem.myColor == myColor)
        //    {
        //        lGem.markForRemoval = true;
        //        markForRemoval = true;
        //    }
        //}

        //check right:
        //if (rGem)
        //{
        //    if (rGem.myColor == myColor)
        //    {
        //        //rGem.markForRemoval = true;
        //        markForRemoval = true;
        //        if (!papaSpawner.gemsToClear.Contains(rGem.transform))
        //        {
        //            papaSpawner.gemsToClear.Add(rGem.transform);
        //        }
        //        //print("Adding " + GameManager.GM.possibleColors[rGem.myColor].name + " " + rGem.xCoord + "," + rGem.yCoord);

        //        if (!papaSpawner.gemsToClear.Contains(transform))
        //        {
        //            papaSpawner.gemsToClear.Add(transform);
        //            //print("Adding " + GameManager.GM.possibleColors[myColor].name+ " " + xCoord + "," + yCoord);

        //        }
        //        rGem.CheckForMatch();
        //    }
        //    else
        //    {
        //        //copy the list over to a new one
        //        if (papaSpawner.gemsToClear.Count >= 3)
        //        {
        //            papaSpawner.CopyList();
        //        }

        //        return;
        //    }
        //}

        //potentially wrong:
        //if (uGem)
        //{
        //    if (uGem.myColor == myColor)
        //    {
        //        uGem.markForRemoval = true;
        //        markForRemoval = true;
        //        if (!papaSpawner.gemsToClear.Contains(uGem.transform))
        //        {
        //            papaSpawner.gemsToClear.Add(uGem.transform);
        //        }
        //        //print("Adding " + rGem.name + " " + rGem.xCoord + "," + rGem.yCoord);

        //        if (!papaSpawner.gemsToClear.Contains(transform))
        //        {
        //            papaSpawner.gemsToClear.Add(transform);
        //            //print("Adding " + name + " " + xCoord + "," + yCoord);

        //        }
        //        uGem.CheckForMatch();
        //    }
        //    else
        //    {
        //        return;
        //    }
        //}

        //papaSpawner.DestroyGems();
    }

    //new match logic chheck:
    public void FindMatches()
    {
        if (xCoord > 0 && xCoord < papaSpawner.cols-1)
        {
            //check left:
            //print(papaSpawner.name);
            //print(papaSpawner.gemsInMe[xCoord - 1, yCoord].GetComponent<GemBehaviour>().myColorName);
            GemBehaviour lDot1 = papaSpawner.gemsInMe[xCoord - 1, yCoord].GetComponent<GemBehaviour>();
            GemBehaviour rDot1 = papaSpawner.gemsInMe[xCoord + 1, yCoord].GetComponent<GemBehaviour>();
            //are the left and right the same color as me?
            if (lDot1.myColor==myColor&& rDot1.myColor == myColor)
            {
                //lDot1.isMatched = true;
                //rDot1.isMatched = true;
                //isMatched = true;
                //lDot1.ChangeColor();
                //rDot1.ChangeColor();
                //ChangeColor();
                papaSpawner.gemsToClear.Add(lDot1);
                papaSpawner.gemsToClear.Add(rDot1);
                papaSpawner.gemsToClear.Add(this);
                papaSpawner.MakingMatchOf(myColorName);
                //lDot1.DestroyMe();
                //rDot1.DestroyMe();
                //DestroyMe();

            }
        }
        if (yCoord > 0 && yCoord < papaSpawner.rows-1)
        {
            //check up:
            GemBehaviour uDot1 = papaSpawner.gemsInMe[xCoord, yCoord+1].GetComponent<GemBehaviour>();
            GemBehaviour dDot1 = papaSpawner.gemsInMe[xCoord, yCoord-1].GetComponent<GemBehaviour>();
            //are the up and down the same color as me?
            if (uDot1.myColor==myColor&& dDot1.myColor == myColor)
            {
                //uDot1.DestroyMe();
                //dDot1.DestroyMe();
                //DestroyMe();
                if (!papaSpawner.gemsToClear.Contains(uDot1))
                {
                    papaSpawner.gemsToClear.Add(uDot1);
                }
                if (!papaSpawner.gemsToClear.Contains(dDot1))
                {
                    papaSpawner.gemsToClear.Add(dDot1);
                }
                if (!papaSpawner.gemsToClear.Contains(this))
                {
                    papaSpawner.gemsToClear.Add(this);
                }
                papaSpawner.MakingMatchOf(myColorName);


            }
        }

        papaSpawner.DestroyGems();
    }

    public void ChangeColor()
    {
        if (myColor < GameManager.GM.possibleColors.Length - 1)
        {
            myColor++;

        }
        else
        {
            myColor = 0;
        }
        GetComponentInChildren<MeshRenderer>().material = GameManager.GM.possibleColors[myColor];
    }
}
