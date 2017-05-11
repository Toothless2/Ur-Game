using UnityEngine;

namespace UrGame
{
    public class Chip : MonoBehaviour
    {
        private Material myMaterial;
        private Vector3 originalChipPos;
        public Color myColour;

        public bool finished;
        public bool isBlue;

        [SerializeField]
        private Vector2[] blueSpaces;
        [SerializeField]
        private Vector2[] blackSpaces;

        public bool onBoard = false;

        private void Start()
        {
            myMaterial = GetComponentInChildren<Renderer>().material;

            myMaterial.color = myColour;

            originalChipPos = transform.position;
        }

        public bool IsChipOutOfBounds(Vector2 position)
        {
            if (finished)
                return false;
            if (isBlue && position.x > 1)
                return true;
            if (!isBlue && position.x < 1)
                return true;

            return false;
        }

        public void ReturnToStart()
        {
            onBoard = false;
            transform.position = originalChipPos;
        }

        public bool RollPastBoard(int roll, int x, int y)
        {
            var spaces = isBlue ? blueSpaces : blackSpaces;

            if (spaces[spaces.Length - roll] == new Vector2(x, y))
                return finished = true;

            return false;
        }

        public bool ValidMove(Chip[] chips, int roll, int x1, int y1, int x2, int y2, bool isBlueTurn,Client c, out int zChange)
        {
            zChange = 0;
            //* checks if the move was valid from the dice roll
            int startPos = -1;

            if (!onBoard)
            {
                startPos = 0;
                roll--;
            }

            var spaces = isBlue ? blueSpaces : blackSpaces;

            for (int i = 0; i < spaces.Length; i++)
            {
                if (spaces[i] == new Vector2(x1, y1))
                    startPos = i;

                if (spaces[i] == new Vector2(x2, y2))
                {
                    if (startPos == -1)
                        return false;

                    if (startPos + roll == i)
                    {
                        //* check all of the chips
                        for (int j = 0; j < chips.Length; j++)
                        {
                            //* if their is a chip already in the space
                            if (chips[j].transform.position == new Vector3(x2, chips[j].transform.position.y, y2))
                            {
                                //* if the chip is not the same colour
                                if (chips[j].isBlue != isBlue)
                                {
                                    //* if the chip is on teh "safe" square
                                    if (chips[j].transform.position == new Vector3(1, 0.55f, 6))
                                    {
                                        for (int h = 0; h < chips.Length; h++)
                                        {
                                            if(new Vector3(chips[h].transform.position.x, 0,chips[h].transform.position.z) == new Vector3(1, 0, 7) && !(chips[h].isBlue == isBlue))
                                            {
                                                chips[h].ReturnToStart();
                                                zChange = 1;
                                                return true;
                                            }
                                        }

                                        //* move the this chip to the poz + 1
                                        zChange = 1;
                                        return true;
                                    }
                                    //* if the enemy chip is not on a safe square remove it from the board
                                    chips[j].ReturnToStart();
                                    c.Send($"RMV|{j}|{x1}|{y1}|{x2}|{y2}|{isBlueTurn}|{Score.blackScore}|{Score.blueScore}");
                                    return true;
                                }

                                return false;
                            }
                        }
                        //* ensure that this chip is on the board and move it
                        onBoard = true;
                        return true;
                    }

                    return false;
                }
            }

            //* if the chip is at the end of the board remove it from play
            if (startPos + roll == spaces.Length)
            {
                if (isBlue)
                    Score.blueScore++;
                else
                    Score.blackScore++;

                return true;
            }

            return false;
        }
    }
}
