﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour
{
    [SerializeField] private float temperature;
    [SerializeField] private float energy;
    [SerializeField] private int gridSize;
    [SerializeField] private Matter[,] grid;
    [SerializeField] private int[,] spiralCoordinates;
    [SerializeField] private Combiner combiner;
    [SerializeField] private GameObject[] spawnableMatter;
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private GameObject cellPrefab;

    private bool updateGame = false;

    // Start is called before the first frame update
    void Start()
    {
        this.createStar();
        this.OutputGridToConsole();
    }

    void createStar()
    {
        this.grid = new Matter[this.gridSize, this.gridSize];
        int center = (this.gridSize - 1) / 2;
        this.createGrid();
        this.generateSpiralCoordinates();
        Matter core = Instantiate(this.corePrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Matter>();
        this.grid[center, center] = core;
        this.spawnRandomMatterInRandomPosition();
        this.spawnRandomMatterInRandomPosition();
        this.redrawMatterInGrid();
    }

    void createGrid()
    {
        int center = (this.gridSize - 1) / 2;
        for (int i = 0; i < this.gridSize; ++i) {
            for (int j = 0; j < this.gridSize; ++j) {
                GameObject cell = Instantiate(this.cellPrefab, new Vector3(j - center, -(i - center), 0), Quaternion.identity);
            cell.transform.parent = this.transform;
            }
        }
    }

    void redrawMatterInGrid()
    {
        int center = (this.gridSize - 1) / 2;
        for (int i = 0; i < this.gridSize; ++i) {
            for (int j = 0; j < this.gridSize; ++j) {
                if (this.grid[i, j] is Matter) {
                    this.grid[i, j].transform.position = new Vector2(j - center, -(i - center));
                }
            }
        }
    }

    void spawnRandomMatterInRandomPosition()
    {
        // find open space and spawn a proton
        // first determine if spawn should happen in top half or bottom half or mid
        // rng is weighted 75% to spawn top half 15% bottom 10% mid
        int rInt = Random.Range(0, 100);
        // rowsFrom/rowsTo defaults to top half
        int rowsFrom = 0;
        int rowsTo = ((this.gridSize - 1) / 2) - 1;

        if (rInt > 75 && rInt <= 90) {
            rowsFrom = ((this.gridSize - 1) / 2) + 1;
            rowsTo = this.gridSize - 1;
        } else if (rInt > 90) {
            rowsFrom = ((this.gridSize - 1) / 2);
            rowsTo = ((this.gridSize - 1) / 2);
        }

        List<int[]> openColumns = this.openSpaces(rowsFrom, rowsTo, 0, this.gridSize-1);

        if (openColumns.Count == 0) {
            // no open spots, try again?
            this.spawnRandomMatterInRandomPosition();
        }

        int index = Random.Range(0, openColumns.Count);

        // spawn protons 90% of the time
        int spawnIndex = 0;
        if (Random.Range(0, 100) >= 90) {
            spawnIndex = 1;
        }
        Matter newMatter = Instantiate(this.spawnableMatter[spawnIndex], new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Matter>();
        this.spawnMatter(newMatter, openColumns[index][0], openColumns[index][1]);
    }

    void spawnMatter(Matter matter, int x, int y)
    {
        this.grid[x, y] = matter;
    }

    public void Rotate(bool clockwise)
    {
        Matter[,] rotated = new Matter[this.gridSize, this.gridSize];

        for (int i = 0; i < this.gridSize; ++i) {
            for (int j = 0; j < this.gridSize; ++j) {
                // 90
                if (clockwise) {
                    rotated[i, j] = this.grid[this.gridSize - j - 1, i];
                } else {
                // -90
                    rotated[i, j] = this.grid[j, this.gridSize - i - 1];
                }
            }
        }

        this.grid = rotated;
        this.updateGame = true;
    }

    void Update()
    {
        if (this.updateGame) {
            this.redrawMatterInGrid();
            this.Fall();
            this.Decay();
            this.spawnRandomMatterInRandomPosition();
            this.redrawMatterInGrid();
            this.OutputGridToConsole();
            this.updateGame = false;
        }
    }

    void Fall()
    {
        for (int i = this.gridSize-2; i >= 0; i--) {
            for (int j = 0; j < this.gridSize; j++) {
                if (this.grid[i, j] is Matter) {
                    if (this.grid[i, j].canMove) {
                        int lowestOpen = i;
                        for (int k = i; k < this.gridSize; k++) {
                            if (!(this.grid[k, j] is Matter)) {
                                if (lowestOpen < k) {
                                    lowestOpen = k;
                                }
                            }
                        }
                        if (lowestOpen != i) {
                            // move to lowestOpen position
                            this.grid[lowestOpen, j] = this.grid[i, j];
                            this.grid[i, j] = null;
                        }

                        if (lowestOpen + 1 < this.gridSize && this.grid[lowestOpen+1, j] is Matter) {
                            if (this.combiner.canCombine(new string[2]{this.grid[lowestOpen, j].name, this.grid[lowestOpen+1, j].name})) {
                                this.Fuse(new Matter[2]{this.grid[lowestOpen, j], this.grid[lowestOpen+1, j]}, lowestOpen, j, lowestOpen+1, j);
                            }
                        }
                    }
                }
            }
        }
    }

    void Fuse(Matter[] matter, int fromX, int fromY, int intoX, int intoY)
    {
        string[] matterNames = new string[matter.Length];
        for (int i = 0; i < matter.Length; i++)
        {
            matterNames[i] = matter[i].name;
        }
        GameObject[] result = this.combiner.result(matterNames);
        if (result != null) {
            Matter merged = Instantiate(result[0], new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Matter>();
            this.spawnMatter(merged, intoX, intoY);
            if (fromX != -1 && fromY != -1) {
                this.grid[fromX, fromY] = null;
            }
            foreach (Matter m in matter)
            {
                m.Fuse();
            }
            // spawn the rest of matter from fusion
            // prioritize open spaces
            // trigger a fusion if possible for adjacent cells with matter
            if (result.Length > 1) {
                for (int i = 1; i < result.Length; i++)
                {
                    Matter mergedOther = Instantiate(result[i], new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Matter>();
                    List<int[]> open = this.openOrthogonalSpaces(intoX, intoY);
                    if (open.Count > 0) {
                        // there is an open orthonal space, spawn that matter there
                        int spawnIndex = Random.Range(0, open.Count);
                        this.spawnMatter(mergedOther, open[spawnIndex][0], open[spawnIndex][1]);
                    } else {
                        // no more spaces, pick a random direction that can fuse
                        open = this.fusableOrthogonalSpaces(mergedOther, intoX, intoY);
                        if (open.Count > 0) {
                            // pick a random direction and trigger fusion
                            int spawnIndex = Random.Range(0, open.Count);
                            this.Fuse(new Matter[2]{mergedOther, this.grid[open[spawnIndex][0], open[spawnIndex][1]]}, -1, -1, open[spawnIndex][0], open[spawnIndex][1]);
                        } else {
                            // no more spaces, pick a random direction to push everything?
                            open = this.orthogonalSpaces(intoX, intoY);
                            int spawnIndex = Random.Range(0, open.Count);
                            int directionX = intoX - open[spawnIndex][0];
                            if (directionX > 0) {
                                directionX = -1;
                            } else if (directionX < 0) {
                                directionX = 1;
                            }
                            int directionY = intoY - open[spawnIndex][1];
                            if (directionY > 0) {
                                directionY = -1;
                            } else if (directionY < 0) {
                                directionY = 1;
                            }
                            this.Push(open[spawnIndex][0], open[spawnIndex][1], directionX, directionY);
                            this.grid[open[spawnIndex][0], open[spawnIndex][1]] = mergedOther;
                        }
                    }
                }
            }
        }
    }

    // Go through grid and decay matter that can decay
    void Decay()
    {
        for (int i = this.gridSize-1; i >= 0; i--) {
            for (int j = 0; j < this.gridSize; j++) {
                if (this.grid[i, j] is Matter) {
                    if (this.combiner.canDecay(this.grid[i, j])) {
                        this.Fuse(new Matter[1]{this.grid[i, j]}, -1, -1, i, j);
                    }
                }
            }
        }
    }

    void Push(int x, int y, int directionX, int directionY)
    {
        // move the item into the cell in the direction
        // if cell is past edge delete matter
        // if cell is occupied call fuse
        if (x + directionX < this.gridSize && x + directionX > 0 && y + directionY < this.gridSize && y + directionY > 0) {
            if (this.grid[x + directionX, y + directionY] is Matter & this.combiner.canCombine(new string[2]{this.grid[x, y].name, this.grid[x + directionX, y + directionY].name})) {
                this.Fuse(new Matter[2]{this.grid[x, y], this.grid[x + directionX, y + directionY]}, x, y, x + directionX, y + directionY);
            }
        } else {
            // delete
        }
    }

    List<int[]> openSpaces(int rowsFrom, int rowsTo, int colsFrom, int colsTo)
    {
        List<int[]> open = new List<int[]>();

        for (int i = rowsFrom; i <= rowsTo; i++)
        {
            for (int j = colsFrom; j <= colsTo; j++)
            {
                if (i < this.gridSize && j < this.gridSize && this.grid[i, j] == null) {
                    open.Add(new int[2] {i, j});
                }
            }
        }

        return open;
    }

    bool hasOpenSpaces()
    {
        return this.openSpaces(0, this.gridSize-1, 0, this.gridSize-1).Count > 0;
    }

    List<int[]> openOrthogonalSpaces(int x, int y)
    {
        List<int[]> open = new List<int[]>();

        if (this.inBounds(x-1, y) && this.grid[x-1, y] == null) {
            open.Add(new int[2] {x-1, y});
        }
        if (this.inBounds(x+1, y) && this.grid[x+1, y] == null) {
            open.Add(new int[2] {x+1, y});
        }
        if (this.inBounds(x, y-1) && this.grid[x, y-1] == null) {
            open.Add(new int[2] {x, y-1});
        }
        if (this.inBounds(x, y+1) && this.grid[x, y+1] == null) {
            open.Add(new int[2] {x, y+1});
        }

        return open;
    }

    List<int[]> orthogonalSpaces(int x, int y)
    {
        List<int[]> open = new List<int[]>();

        if (this.inBounds(x-1, y)) {
            open.Add(new int[2] {x-1, y});
        }
        if (this.inBounds(x+1, y)) {
            open.Add(new int[2] {x+1, y});
        }
        if (this.inBounds(x, y-1)) {
            open.Add(new int[2] {x, y-1});
        }
        if (this.inBounds(x, y+1)) {
            open.Add(new int[2] {x, y+1});
        }

        return open;
    }

    List<int[]> fusableOrthogonalSpaces(Matter matter, int x, int y)
    {
        List<int[]> open = new List<int[]>();

        if (this.inBounds(x-1, y) && this.grid[x-1, y] is Matter && this.combiner.canCombine(new string[2]{matter.name, this.grid[x-1, y].name})) {
            open.Add(new int[2] {x-1, y});
        }
        if (this.inBounds(x+1, y) && this.grid[x+1, y] is Matter && this.combiner.canCombine(new string[2]{matter.name, this.grid[x+1, y].name})) {
            open.Add(new int[2] {x+1, y});
        }
        if (this.inBounds(x, y-1) && this.grid[x, y-1] is Matter && this.combiner.canCombine(new string[2]{matter.name, this.grid[x, y-1].name})) {
            open.Add(new int[2] {x, y-1});
        }
        if (this.inBounds(x, y+1) && this.grid[x, y+1] is Matter && this.combiner.canCombine(new string[2]{matter.name, this.grid[x, y+1].name})) {
            open.Add(new int[2] {x, y+1});
        }

        return open;
    }

    bool inBounds(int x, int y) {
        return x < this.gridSize && x >= 0 && y < this.gridSize && y >= 0;
    }

    void OutputGridToConsole()
    {
        string output = "";
        for (int i = 0; i < this.gridSize; i++) {
            for (int j = 0; j < this.gridSize; j++) {
                if (this.grid[i, j] is Matter) {
                    output += this.grid[i, j].name.Substring(0, 1) + " ";
                } else {
                    output += "- ";
                }
            }
            output += "\n";
        }
        Debug.Log(output);
    }

    /// <summary>
    /// Converts the 2d matrix into a single array of coordinates that
    /// will loop through the matrix in a spiral COUNTERCLOCKWISE
    /// only works for a odd length square matrix
    /// </summary>
    void generateSpiralCoordinates()
    {
        this.spiralCoordinates = new int[this.gridSize*this.gridSize, 2];
        int center = (this.gridSize - 1) / 2;
        int currentX = center;
        int currentY = center;
        int position = 0;
        int step = 0;
        bool add = false;
        bool hasNext = true;
        while (hasNext) {
            if (step == 0) {
                // if this is the first step we are at the center
                // just put the center coords into the spiral array
                // and then the coord to the right of center
                this.spiralCoordinates[position, 0] = center;
                this.spiralCoordinates[position, 1] = center;
                position++;
                currentY = center+1;
                this.spiralCoordinates[position, 0] = center;
                this.spiralCoordinates[position, 1] = currentY;
                position++;
                step++;
            } else {
                // after the first step we go in the order
                // increase x step times
                // increase y step+1 times
                for (int i = 0; i < step; i++)
                {
                    if (add) {
                        currentX += 1;
                    } else {
                        currentX -= 1;
                    }
                    if (currentX < this.gridSize && currentY < this.gridSize) {
                        this.spiralCoordinates[position, 0] = currentX;
                        this.spiralCoordinates[position, 1] = currentY;
                    }
                    position++;
                }
                for (int i = 0; i <= step; i++)
                {
                    if (add) {
                        currentY += 1;
                    } else {
                        currentY -= 1;
                    }
                    if (currentX < this.gridSize && currentY < this.gridSize) {
                        this.spiralCoordinates[position, 0] = currentX;
                        this.spiralCoordinates[position, 1] = currentY;
                    }
                    position++;
                }
                add = !add;
                step++;
            }
            if (currentX >= this.gridSize || currentY >= this.gridSize) {
                hasNext = false;
            }
        }
    }
}