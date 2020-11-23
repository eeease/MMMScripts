using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    public List<Transform> columns;
    public List<Vector3> columnSpawnPos, m_columnSpawnPos;
    public float spawnBuffer = 3f;

    public GameObject gemToSpawn;
    public int rows, cols;
    public float spawnDel = .5f;

    public List<GemBehaviour> gemsToClear;
    public int gtcInd;

    public Transform[,] gemsInMe;
    Transform[,] toTest;

    public int reserveToSpawn;

    bool stagger = false;
    public bool refilling = false;

    // Start is called before the first frame update
    void Start()
    {
        //gemsToClearArray = new List<Transform>()[30];
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (!t.Equals(transform))
            {
                columns.Add(t);
                columnSpawnPos.Add(t.position);
                m_columnSpawnPos.Add(t.position);
            }
        }
        cols = columns.Count;
        gemsInMe = new Transform[cols, rows];
        toTest = new Transform[cols, rows];//blank now for testing;
        StartCoroutine(PopulateBoardInital());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
           // CheckForMatches();
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            //for(int i=0; i<m_columnSpawnPos.Count; i++)
            //{
            //    m_columnSpawnPos[i] = columnSpawnPos[i];
            //}
        }

        if (Input.GetKeyDown(KeyCode.O))
            OutputArray();

        //Debug.Log(refilling);
        if (refilling)
        {
            CheckVelocities();
        }
        //if (refilling)
        //{
        //    CheckColumnNumbers();

        //}
    }

    public void ResetSpawnPoses()
    {
        for (int i = 0; i < m_columnSpawnPos.Count; i++)
        {
            m_columnSpawnPos[i] = columnSpawnPos[i];
        }
    }

    //public void CheckColumnNumbers()
    //{
    //    foreach(Transform c in columns)
    //    {
    //        if (c.GetComponentsInChildren<Transform>().Length < 11)
    //        {
    //            StartCoroutine(SpawnGemInColumn(columns.IndexOf(c)));
    //        }
    //    }
    //}

        //this whole thing is bad and wrong and causing problems:
    public void CheckVelocities()
    {
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if (gemsInMe[i, j])
                {
                    if (Mathf.Abs(gemsInMe[i, j].GetComponent<Rigidbody>().velocity.y) > 0.1)
                    {
                        refilling = true;
                        return;
                    }
                }
              

                    
            }
        }
        refilling = false;
        ResetSpawnPoses();
        StartCoroutine(MatchCheck());
    }

    public IEnumerator PopulateBoardInital()
    {
        refilling = true;
        for (int i = 0; i < cols; i++)
        {
            for(int j =0; j<rows; j++)
            {
                GameObject t = Instantiate(gemToSpawn, columns[i], false);
                GemBehaviour gb = t.GetComponent<GemBehaviour>();
                gemsInMe[i, j] = t.transform;

                gb.xCoord = i;
                gb.yCoord = j;
                gb.papaSpawner = this;
                SetGemColor(gb);
                
                yield return new WaitForSeconds(spawnDel);
            }
        }
        //refilling = false;
    }

    public void SetGemColor(GemBehaviour whichGem)
    {
        whichGem.SetColor(GameManager.GM.colorsToSpawn[reserveToSpawn]);

        if (reserveToSpawn < GameManager.GM.colorsToSpawn.Length-1)
        {
            reserveToSpawn++;
        }
        else
        {
            reserveToSpawn = 0;
        }
    }

    public void RemoveGem(int x, int y)
    {
        //print(GameManager.GM.possibleColors[gemsInMe[x, y].GetComponent<GemBehaviour>().myColor].name + " removed");
        //remove it from its spot in the 2DA:
        //print("goodbye " + gemsInMe[x, y].GetComponentInChildren<MeshRenderer>().material.name);
        gemsInMe[x, y] = null;
        //go up the column and see if there's a gem there.  if so, put that gem in the now-null spot:
        for(int i = y; i<rows-1; i++)
        {
            if (gemsInMe[x, i+1] != null)
            {
                gemsInMe[x, i] = gemsInMe[x, i+1];
                gemsInMe[x, i + 1] = null; 
                //print("hello " + gemsInMe[x, i].GetComponentInChildren<MeshRenderer>().material.name);
                //gemsInMe[x, i].GetComponent<GemBehaviour>().yCoord = i;
            }
          
        }
        StartCoroutine(SpawnGemInColumn(x));
        //loop through and update all the other gems in this column:
        //for (int i = y; i < rows - 1; i++)
        //{
        //    //'drop' the higher gem
        //    gemsInMe[x, i] = gemsInMe[x, i + 1];
        //    gemsInMe[x, i].GetComponent<GemBehaviour>().yCoord--;
        //}
        //print(GameManager.GM.possibleColors[gemsInMe[x, y].GetComponent<GemBehaviour>().myColor].name);

    }
    public void Placeholder(int num)
    {
        StartCoroutine(SpawnGemInColumn(num));
    }
    public IEnumerator SpawnGemInColumn(int whichColumn)
    {
        //float wait = 0;
        //if (stagger)
        //{
        //     wait = 1;

        //}
        yield return new WaitForSeconds(spawnDel);
        //GameObject t = Instantiate(gemToSpawn, columns[whichColumn], false);
        GameObject t = Instantiate(gemToSpawn, m_columnSpawnPos[whichColumn], Quaternion.identity, columns[whichColumn]);
        m_columnSpawnPos[whichColumn] += Vector3.up * spawnBuffer;
        GemBehaviour gb = t.GetComponent<GemBehaviour>();
        gb.xCoord = whichColumn;
        //go through the gemsInMe 2DA and set the gem's ycoord in the first free spot from the bottom
        for (int i = 0; i < rows; i++)
        {
            //find the first open spot in the column.
            //maybe redundant?
            if (gemsInMe[whichColumn, i] == null)
            {
               // print("this was null: " + whichColumn + ", " + i);
                gemsInMe[whichColumn, i] = gb.transform;
                //gb.yCoord = i;
            }
        }
        //gb.yCoord = rows - 1;
        //gemsInMe[whichColumn, rows - 1] = t.transform;

        //this seems really sledgehammer-y but it's working and nothing else that i've tried has worked:
        foreach (Transform c in columns)
        {
            List<GemBehaviour> tempList = new List<GemBehaviour>();
            tempList.AddRange(c.GetComponentsInChildren<GemBehaviour>());
            tempList.Sort((x, y) => x.startTime.CompareTo(y.startTime));
            for (int i = 0; i < tempList.Count; i++)
            {
                tempList[i].yCoord = i;
                gemsInMe[columns.IndexOf(c), i] = tempList[i].transform;
            }
        }

        gb.papaSpawner = this;
        SetGemColor(gb);
        refilling = true;
        //stagger = !stagger;
    }

    public void CheckForMatches()
    {

        for(int i=0; i<cols; i++)
        {
            for(int j=0; j<rows; j++)
            {
                //if(gemsInMe[i,j])
                gemsInMe[i, j].GetComponent<GemBehaviour>().CheckForMatch();
            }
        }
        DestroyGems();
    }

    public IEnumerator MatchCheck()
    {
        yield return new WaitForSeconds(.5f);
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if(gemsInMe[i,j])
                gemsInMe[i, j].GetComponent<GemBehaviour>().FindMatches();
            }
        }
    }

    public void OutputArray()
    {
        Debug.ClearDeveloperConsole();
        Debug.Log("");

        for (int i = rows-1; i > -1; i--)
        {
            string output = "";

            for (int j = 0; j <cols; j++)
            {
                //int colorIndex = gemsInMe[j, i].GetComponent<GemBehaviour>().myColor;
                //char[] colorLetters = GameManager.GM.possibleColors[colorIndex].name.ToCharArray();
                output += gemsInMe[j,i].GetComponent<GemBehaviour>().myColorName + " ";
            }

            Debug.Log(output);
        }

        Debug.Log("");
    }

    public void MakingMatchOf(string color)
    {
        print("made a match of " + color);

        //ObjectsManager.instance.SpawnEnemy(transform.parent.tag, color);
    }
    public void DestroyGems()
    {
        //check if multiple in the same column are being removed:

        refilling = true;


        //print("COUNT = " + gemsToClear.Count);
        if (gemsToClear.Count >= 3)
        {
            foreach (GemBehaviour t in gemsToClear)
            {
                t.DestroyMe();
            }
            gemsToClear.Clear();

        }
        //else
        //{
        //    gemsToClear.Clear();
        //    //print("no matches");
        //}
    }

    //public void CopyBoard(Transform[,] source, Transform[,] dest)
    //{
    //    for (int x = 0; x<cols; x++)
    //    {
    //        for (int y = 0; y<rows; y++)
    //        {
    //            dest[x, y] = source[x, y];
    //        }
    //    }
    //}

    //public void CheckForMatches(Transform[,] board)
    //{
    //    CopyBoard(gemsInMe, toTest);
        
    //}
}
