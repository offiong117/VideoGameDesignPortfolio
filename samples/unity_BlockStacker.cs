// BlockStacker.cs
// Core logic for a two-player block-stacking game (Unity, PC + iPad).
//
// Each player owns a 10x18 grid. Pieces fall on a fixed tick; clearing rows
// sends "garbage" rows to the other player. Input is unified so the same
// script handles keyboard on desktop and on-screen swipes/taps on iPad.
//
// Drop on a GameObject per player; set `playerIndex` 0 or 1.
// Wire a child Transform `gridOrigin` for world-space rendering.

using System.Collections.Generic;
using UnityEngine;

public class BlockStacker : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 18;
    public float cellSize = 0.5f;
    public Transform gridOrigin;

    [Header("Tick")]
    public float baseFallInterval = 0.6f;
    public float softDropMultiplier = 6f;

    [Header("Player")]
    [Range(0, 1)] public int playerIndex = 0;
    public BlockStacker opponent;       // assigned in inspector; nullable

    [Header("Rendering")]
    public GameObject cellPrefab;       // small unit cube w/ MeshRenderer

    // --- runtime state ---
    int[,] _grid;                       // 0 = empty, >0 = colorId
    Piece _piece;
    float _tickAccum;
    bool _gameOver;
    InputAxis _input;

    void Awake()
    {
        _grid = new int[width, height];
        _input = new InputAxis(playerIndex);
        SpawnPiece();
        RenderAll();
    }

    void Update()
    {
        if (_gameOver) return;

        _input.Poll();
        if (_input.Left)     TryMove(-1, 0);
        if (_input.Right)    TryMove(+1, 0);
        if (_input.Rotate)   TryRotate();
        if (_input.HardDrop) HardDrop();

        float interval = baseFallInterval / (_input.SoftDropHeld ? softDropMultiplier : 1f);
        _tickAccum += Time.deltaTime;
        while (_tickAccum >= interval)
        {
            _tickAccum -= interval;
            if (!TryMove(0, -1)) Lock();
        }
        RenderAll();
    }

    // --- mechanics ---
    bool TryMove(int dx, int dy)
    {
        var p = _piece.Shifted(dx, dy);
        if (Fits(p)) { _piece = p; return true; }
        return false;
    }

    void TryRotate()
    {
        var rotated = _piece.Rotated();
        // simple wall-kick: try center, then +/-1
        foreach (int kick in new[] { 0, -1, 1, -2, 2 })
        {
            var test = rotated.Shifted(kick, 0);
            if (Fits(test)) { _piece = test; return; }
        }
    }

    void HardDrop()
    {
        while (TryMove(0, -1)) { /* slam */ }
        Lock();
    }

    void Lock()
    {
        foreach (var c in _piece.Cells())
            if (c.y < height) _grid[c.x, c.y] = _piece.colorId;

        int cleared = ClearFullRows();
        if (cleared > 1 && opponent != null) opponent.ReceiveGarbage(cleared - 1);
        if (!SpawnPiece()) _gameOver = true;
    }

    int ClearFullRows()
    {
        int writeY = 0;
        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++) if (_grid[x, y] == 0) { full = false; break; }
            if (!full)
            {
                if (writeY != y)
                    for (int x = 0; x < width; x++) _grid[x, writeY] = _grid[x, y];
                writeY++;
            }
        }
        int cleared = height - writeY;
        for (int y = writeY; y < height; y++)
            for (int x = 0; x < width; x++) _grid[x, y] = 0;
        return cleared;
    }

    public void ReceiveGarbage(int rows)
    {
        // shift everything up, fill the bottom rows with a single gap
        for (int y = height - 1; y >= rows; y--)
            for (int x = 0; x < width; x++) _grid[x, y] = _grid[x, y - rows];
        int gap = Random.Range(0, width);
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < width; x++) _grid[x, y] = (x == gap) ? 0 : 8;
    }

    bool SpawnPiece()
    {
        _piece = Piece.Random(width);
        return Fits(_piece);
    }

    bool Fits(Piece p)
    {
        foreach (var c in p.Cells())
        {
            if (c.x < 0 || c.x >= width || c.y < 0) return false;
            if (c.y < height && _grid[c.x, c.y] != 0) return false;
        }
        return true;
    }

    void RenderAll()
    {
        // omitted for brevity: pool cellPrefab instances per grid+piece cell
        // and position them at gridOrigin + (x, y) * cellSize.
    }
}
