using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//this script will actually check for matches, instead of
//every single dot looking for matches.
public class FindMatches : MonoBehaviour
{
    public Board board;
    public List<Gem> currentMatches;
    int[] colorsMatched;
    string[] colorNames;

    public GameObject numberCan;
    public List<Sprite> numbers;
    public Transform numberSpawnPos;


    // Start is called before the first frame update
    void Start()
    {
        colorsMatched = new int[GameManager.GM.possibleColors.Length];
        colorNames = new string[GameManager.GM.possibleColors.Length];
        for(int i=0; i<GameManager.GM.possibleColors.Length; i++)
        {
            colorNames[i] = GameManager.GM.possibleColors[i].name;
        }
        if (board == null)
        {
            board = GetComponent<Board>(); //this should be placed on the board that it's going to look at;
        }
    }

    public void FindAllMatches()
    {
        StartCoroutine(FindAllMatchesCo());
    }

    private IEnumerator FindAllMatchesCo()
    {
        //print("find all matches Co");
        yield return new WaitForSeconds(board.checkDelay);
        for(int i = 0; i<board.width; i++)
        {
            for(int j=0; j<board.height; j++)
            {
                GameObject cG = board.allDots[i, j];
                if (cG != null)
                {
                    Gem currentGem = cG.GetComponent<Gem>();
                    int tempIndex = currentGem.myColor;
                    if (i > 0 && i < board.width - 1)
                    {
                        //had this as Gem lDot =...GetComponent<Gem>() but that was throwing errors...
                        GameObject lDot = board.allDots[i - 1, j];
                        GameObject rDot = board.allDots[i + 1, j];
                        if(lDot!=null && rDot != null)
                        {
                            Gem lD = lDot.GetComponent<Gem>();
                            Gem rD = rDot.GetComponent<Gem>();
                            if ((lD.myColor==currentGem.myColor) && (rD.myColor == currentGem.myColor)){
                                if (!currentMatches.Contains(lD))
                                {
                                    currentMatches.Add(lD);
                                    colorsMatched[tempIndex]++;
                                }
                                lD.isMatched = true;

                                if (!currentMatches.Contains(rD))
                                {
                                    currentMatches.Add(rD);
                                    colorsMatched[tempIndex]++;

                                }
                                rD.isMatched = true;

                                if (!currentMatches.Contains(currentGem))
                                {
                                    currentMatches.Add(currentGem);
                                    colorsMatched[tempIndex]++;

                                }
                                currentGem.isMatched = true;
                            }
                        }
                    }

                    //now do vertical:
                    if (j > 0 && j < board.height - 1)
                    {
                        GameObject uDot = board.allDots[i, j+1];
                        GameObject dDot = board.allDots[i, j-1];
                        if (uDot != null && dDot != null)
                        {
                            Gem uD = uDot.GetComponent<Gem>();
                            Gem dD = dDot.GetComponent<Gem>();
                            if ((uD.myColor == currentGem.myColor) && (dD.myColor == currentGem.myColor)){
                                if (!currentMatches.Contains(uD))
                                {
                                    currentMatches.Add(uD);
                                    colorsMatched[tempIndex]++;

                                }
                                uD.isMatched = true;

                                if (!currentMatches.Contains(dD))
                                {
                                    currentMatches.Add(dD);
                                    colorsMatched[tempIndex]++;

                                }
                                dD.isMatched = true;

                                if (!currentMatches.Contains(currentGem))
                                {
                                    currentMatches.Add(currentGem);
                                    colorsMatched[tempIndex]++;

                                }
                                currentGem.isMatched = true;
                            }
                        }
                    }
                }
            }
        }

        //this should discern how many of each color are being matched.
        //print(currentMatches.Count);
        //print("blues: " + colorsMatched[0]);
        //print("yellows: " + colorsMatched[1]);
        //print("greens: " + colorsMatched[2]);
        //print("blacks: " + colorsMatched[3]);
        //print("purples: " + colorsMatched[4]);
        //print("reds: " + colorsMatched[5]);
        if(currentMatches.Count>0)
        board.DestroyMatches();
        CheckColorMatches();
        ClearColorInts();
    }

    private void CheckColorMatches()
    {
        for (int i=0; i<colorNames.Length; i++)
        {
            if (colorsMatched[i] > 0)
            {
                board.MakingMatchOf(i, colorsMatched[i]);
                board.comboNumber++;
                //print(colorNames[i] + "s: " + colorsMatched[i]);
                GameObject img = Instantiate(numberCan, numberSpawnPos.position, Quaternion.identity);
                img.transform.SetParent(transform);
                img.transform.localRotation = Quaternion.Euler(Vector3.zero);
                img.GetComponentInChildren<Image>().sprite = numbers[colorsMatched[i]];

                img.GetComponentInChildren<Image>().color = GameManager.GM.actualColors[i];

                //!also trigger a mega match notif here:

            }
        }
    }

    private void ClearColorInts()
    {
        for(int i=0; i<colorsMatched.Length; i++)
        {
            colorsMatched[i] = 0;
        }
    }

}
