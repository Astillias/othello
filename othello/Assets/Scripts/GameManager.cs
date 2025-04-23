using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform boardParent;
    public AudioSource audioSource;
    public AudioClip placementSound; private bool isAnimating = false;

    private TokenColor currentTurn = TokenColor.Black;
    private Tile[,] board = new Tile[8, 8];
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(-1, -1),
        new Vector2Int(-1, 1), new Vector2Int(1, -1)
    }; 
    public static GameManager Instance;

    void Awake()
    {
        Instance = this;
        if (winScreen != null) winScreen.SetActive(false);

        if (mouseToken == null && canvas != null)
        {
            GameObject tokenObj = Instantiate(tilePrefab, canvas.transform).gameObject;
            mouseToken = tokenObj.GetComponent<RectTransform>();
        }
    }

    void Start()
    {
        InitBoard();
    }

    public RectTransform mouseToken; 
    public Sprite blackTokenSprite;
    public Sprite whiteTokenSprite;
    public Canvas canvas;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetGame();

        if (mouseToken != null && canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; 

            mouseToken.transform.position = mouseWorldPos;
            mouseToken.GetComponent<SpriteRenderer>().sprite = currentTurn == TokenColor.Black ? blackTokenSprite : whiteTokenSprite;
        }
    }

    void InitBoard()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                GameObject obj = Instantiate(tilePrefab, new Vector3((1.25f * x) - 4.4f, (1.25f * y) - 4.4f, 0), Quaternion.identity, boardParent);
                Tile tile = obj.GetComponent<Tile>();
                board[x, y] = tile;
                tile.SetToken(TokenColor.Empty);
            }
        }

        board[3, 3].SetToken(TokenColor.White);
        board[4, 4].SetToken(TokenColor.White);
        board[3, 4].SetToken(TokenColor.Black);
        board[4, 3].SetToken(TokenColor.Black);
    }

    public void OnTileClicked(Tile tile)
    {
        if (isAnimating) return;
        Vector2Int pos = GetTilePosition(tile);
        if (!tile.IsEmpty()) return;
        List<Tile> flips = GetFlippableTiles(pos, currentTurn);
        if (flips.Count == 0) return;

        StartCoroutine(PlaceAndFlip(tile, flips));
    }

    IEnumerator PlaceAndFlip(Tile tile, List<Tile> flips)
{
    isAnimating = true;

    tile.SetToken(currentTurn);
    audioSource.PlayOneShot(placementSound);

    // Group flips by distance to the placed tile
    Vector2Int centerPos = GetTilePosition(tile);
    Dictionary<int, List<Tile>> distanceGroups = new Dictionary<int, List<Tile>>();

    foreach (Tile t in flips)
    {
        Vector2Int tPos = GetTilePosition(t);
        int dist = Mathf.Max(Mathf.Abs(tPos.x - centerPos.x), Mathf.Abs(tPos.y - centerPos.y));
        if (!distanceGroups.ContainsKey(dist))
            distanceGroups[dist] = new List<Tile>();
        distanceGroups[dist].Add(t);
    }

    // Flip tiles in groups, one group per frame (cascade effect)
    List<int> distances = new List<int>(distanceGroups.Keys);
    distances.Sort();

    foreach (int dist in distances)
    {
        List<Tile> group = distanceGroups[dist];
        foreach (Tile t in group)
        {
            t.SetToken(currentTurn);
        }
        yield return new WaitForSeconds(0.1f); // delay between groups
    }

    yield return new WaitUntil(() => AllAnimationsFinished());

    isAnimating = false;
    SwitchTurn();
}

    bool AllAnimationsFinished()
    {
        foreach (Tile tile in board)
        {
            if (tile.isAnimating) return false;
        }
        return true;
    }




    Vector2Int GetTilePosition(Tile tile)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (board[x, y] == tile)
                    return new Vector2Int(x, y);
            }
        }
        return Vector2Int.zero;
    }

    List<Tile> GetFlippableTiles(Vector2Int pos, TokenColor color)
    {
        List<Tile> result = new List<Tile>();
        foreach (Vector2Int dir in directions)
        {
            List<Tile> line = new List<Tile>();
            int x = pos.x + dir.x;
            int y = pos.y + dir.y;

            while (IsInBounds(x, y) && board[x, y].tokenColor != TokenColor.Empty && board[x, y].tokenColor != color)
            {
                line.Add(board[x, y]);
                x += dir.x;
                y += dir.y;
            }

            if (IsInBounds(x, y) && board[x, y].tokenColor == color)
                result.AddRange(line);
        }
        return result;
    }

    bool IsInBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < 8 && y < 8;
    }

    IEnumerator FlipTiles(List<Tile> tiles, TokenColor color)
    {
        foreach (Tile tile in tiles)
        {
            yield return new WaitForSeconds(0.1f);
            tile.SetToken(color);
        }

        isAnimating = false;
    }
    bool IsBoardFull()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (board[x, y].tokenColor == TokenColor.Empty)
                    return false;
            }
        }
        return true;
    }

    void SwitchTurn()
    {
        if (IsBoardFull())
        {
            EndGame();
            return;
        }
        TokenColor nextTurn = currentTurn == TokenColor.Black ? TokenColor.White : TokenColor.Black;

        if (HasValidMove(nextTurn))
        {
            currentTurn = nextTurn;
        }
        else
        {
            if (!HasValidMove(currentTurn))
            {
                EndGame();
            }
            else
            {
                Debug.Log($"{nextTurn} has no valid moves! {currentTurn} plays again");
            }
        }
    }

    bool HasValidMove(TokenColor color)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (board[x, y].tokenColor == TokenColor.Empty)
                {
                    if (GetFlippableTiles(new Vector2Int(x, y), color).Count > 0)
                        return true;
                }
            }
        }
        return false;
    }

    public GameObject winScreen;
    public UnityEngine.UI.Text winText;

    void EndGame()
    {
        StartCoroutine(ShowWinScreen());
    }

    IEnumerator ShowWinScreen()
    {
        yield return new WaitUntil(() => AllAnimationsFinished());
        yield return new WaitForSeconds(0.3f);

        int black = 0;
        int white = 0;

        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                if (board[x, y].tokenColor == TokenColor.Black) black++;
                else if (board[x, y].tokenColor == TokenColor.White) white++;
            }

        winScreen.SetActive(true);
        if (black > white) winText.text = $"BLACK WINS!\n{black}-{white}";
        else if (white > black) winText.text = $"WHITE WINS!\n{white}-{black}";
        else winText.text = $"TIE GAME!\n{black}-{white}";
    }


    void ResetGame()
    {
        winScreen.SetActive(false);
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        currentTurn = TokenColor.Black;
        InitBoard();
    }
}
