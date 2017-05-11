using System;
using UnityEngine;

namespace UrGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager managerInstance;

        public Chip[] chips = new Chip[14];
        public GameObject chipPrefab;
        public LayerMask dragLayers;
        public UnityEngine.UI.Text rollText;
        private Vector2 mousePosition;

        public int rollNumber = 0;
        public bool isBlueTurn;

        public bool isBlue;

        private Chip selectedChip;
        private Vector2 startDrag;
        private Vector2 endDrag;

        public Client client;

        private void Start()
        {
            managerInstance = this;
            client = FindObjectOfType<Client>();
            isBlue = client.isHost;

            SpawnPieces();
        }

        private void Update()
        {
            UpdateMouseOver();

            if (rollNumber == 0)
                return;
            //* if it is my turn
            {
                if(Input.GetMouseButtonDown(0))
                    SelectPiece((int)mousePosition.x, (int)mousePosition.y);

                if (Input.GetMouseButton(0) && selectedChip != null)
                    UpdateChipDrag(selectedChip);

                if (Input.GetMouseButtonUp(0) && selectedChip != null)
                    TryMove((int)startDrag.x, (int)startDrag.y, (int)mousePosition.x, (int)mousePosition.y);
            }
        }

        public void StartTurn(bool turn, int blackScore, int blueScore)
        {
            rollNumber = 0;
            rollText.text = $"{rollNumber}";
            Score.blueScore = blueScore;
            Score.blackScore = blackScore;
            isBlueTurn = turn;
        }

        public void RollDice()
        {
            rollNumber = 0;

            var rand = new System.Random();

            for (int i = 0; i < 4; i++)
            {
                var temp = rand.Next(1, 5);

                if (temp >= 3)
                {
                    rollNumber++;
                }
            }

            if (rollNumber == 0)
            {
                NextTurn();
                client.Send($"TUR|{0}|{0}|{0}|{0}|{isBlueTurn}|{Score.blackScore}|{Score.blueScore}");
            }

            rollText.text = $"{rollNumber}";
        }

        private void UpdateMouseOver()
        {
            //* if it is my turn
            if(!Camera.main)
            {
                throw new Exception("No Main Camera!? Hot did you do this!?");
            }

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, LayerMask.GetMask("Board")))
            {
                mousePosition.x = (int)(hit.point.x + 0.5f);
                mousePosition.y = (int)(hit.point.z + 0.5f);
            }
            else
            {
                mousePosition = new Vector2(-20, -20);
            }
        }
        private void UpdateChipDrag(Chip c)
        {
            if (!Camera.main)
            {
                throw new Exception("No Main Camera!? How did you do this!?");
            }

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, dragLayers))
            {
                c.transform.position = hit.point + (Vector3.up * 0.6f);
            }
        }

        public void SelectPiece(int x, int y)
        {
            Chip c;

            for (int i = 0; i < chips.Length; i++)
            {
                Vector2 temp = new Vector2((int)(chips[i].transform.position.x + 0.5f), (int)(chips[i].transform.position.z + 0.5f));

                if (temp == mousePosition)
                {
                    if (!(chips[i].isBlue == isBlue))
                        return;
                    selectedChip = c = chips[i];
                    startDrag = new Vector2(selectedChip.transform.position.x, selectedChip.transform.position.z);
                    return;
                }
            }


            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, LayerMask.GetMask("Chip")))
            {
                if (hit.transform == null)
                    return;
                if (hit.transform.GetComponent<Chip>().isBlue != isBlueTurn)
                    return;
                if (hit.transform.GetComponent<Chip>().isBlue != isBlue)
                    return;

                selectedChip = c = hit.transform.GetComponent<Chip>();
                startDrag = new Vector2(selectedChip.transform.position.x, selectedChip.transform.position.z);

                //print(selectedChip);
                return;
            }

            //selectedChip = null;
            startDrag = new Vector2();
        }
        public void TryMove(int x1, int y1, int x2, int y2)
        {
            startDrag = new Vector2(x1, y1);
            endDrag = new Vector2(x2, y2);

            //* check if out of bounds
            if (mousePosition == new Vector2(-20, -20) && !selectedChip.RollPastBoard(rollNumber, x1, y1))
            {
                ResetChipPosition(x1, y1);
                return;
            }
            else if ((bool)selectedChip?.IsChipOutOfBounds(endDrag))
            {
                ResetChipPosition(x1, y1);
                return;
            }


            //* if the chip has not moved
            if (endDrag == startDrag)
            {
                ResetChipPosition(x1, y1);
                return;
            }
            //* if the chip has done a valid move
            if (selectedChip.ValidMove(chips, rollNumber, x1, y1, x2, y2, isBlueTurn,client, out int zChange))
            {
                rollNumber = 0;
                selectedChip.transform.position = new Vector3(x2, 0.55f, y2 + zChange);

                if (new Vector3(selectedChip.transform.position.x, 0, selectedChip.transform.position.z) != new Vector3(2, 0, 3) && new Vector3(selectedChip.transform.position.x, 0, selectedChip.transform.position.z) != new Vector3(0, 0, 3))
                    NextTurn();

                //rollText.text = "TUR sent from gm";
                client.Send($"TUR|{x1}|{y1}|{x2}|{y2 + zChange}|{isBlueTurn}|{Score.blackScore}|{Score.blueScore}");
                selectedChip = null;
                startDrag = new Vector2();
                endDrag = new Vector2();
            }
            else
            {
                ResetChipPosition(x1, y1);
            }
        }
        public void MoveingPiece(float x1, float y1, float x2, float y2)
        {
            for (int i = 0; i < chips.Length; i++)
            {
                if (new Vector2(chips[i].transform.position.x, chips[i].transform.position.z) == new Vector2(x1, y1))
                {
                    chips[i].transform.position = new Vector3(x2, 0.55f, y2);
                    return;
                }
            }
        }
        private void ResetChipPosition(int x, int y)
        {

            if(x <= 2 && x >= 0 && y <= 10 && y >= 3)
            {
                selectedChip.transform.position = new Vector3(x, 0.55f, y);
            }
            else
            {
                selectedChip.transform.position = new Vector3(x, 0, y);
            }

            selectedChip = null;
            startDrag = new Vector2();
            endDrag = new Vector2();
        }
        public void SkipTurn()
        {
            client.Send($"TUR|{0}|{0}|{0}|{0}|{isBlueTurn}|{Score.blackScore}|{Score.blueScore}");
        }
        public void NextTurn()
        {
            isBlueTurn = !isBlueTurn;
            rollText.text = $"{rollNumber}";
            CheckVictory();
        }
        public void CheckVictory()
        {

        }

        private void SpawnPieces()
        {
            //* spawns black chips
            for (int i = 0; i < 7; i++)
            {
                GameObject c = Instantiate(chipPrefab);
                c.name = "black";
                c.transform.position = new Vector3(4f, 0, i + 3f);

                Chip chip = c.GetComponent<Chip>();
                chip.myColour = Color.black;

                chips[i] = chip;
            }

            //* spawns blue chips
            for (int i = 0; i < 7; i++)
            {
                GameObject c = Instantiate(chipPrefab);
                c.name = "blue";
                c.transform.position = new Vector3(-2f, 0, i + 3f);

                Chip chip = c.GetComponent<Chip>();
                chip.myColour = Color.blue;
                chip.isBlue = true;

                chips[i + 7] = chip;
            }
        }
    }
}
