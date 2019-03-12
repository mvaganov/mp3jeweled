using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jewelmatcher : MonoBehaviour
{
    public Vector2Int size;
    public Vector2Int gravDir = Vector2Int.down;
    public float swapDistance = 1;
    public float swapSpeed = 10;
    public float selectedScale = 1.25f;
    public int thisManyInARowToActivate = 3;
    bool dirty = true;

    Jewel[,] board;
    Jewel selected;
    // Start is called before the first frame update

    public ParticleSystem ps;
    public Jewel[] possibleJewels;
    List<Vector2Int> freeSpaces;

    public List<Vector2Int> GetFreeSpaces()
    {
        List<Vector2Int> spaces = new List<Vector2Int>();
        for (int r = 0; r < size.y; ++r)
        {
            for (int c = 0; c < size.x; ++c)
            {
                if(board[r,c] == null)
                {
                    spaces.Add(new Vector2Int(c, r));
                }
            }
        }
        return spaces;
    }

    public Vector3 GetCenter()
    {
        Vector3 off = (Vector2)size / -2;
        return transform.position + new Vector3(0.5f, 0.5f, 0) + off;
    }

    public Vector3 GetPosition(Vector2Int rowcol)
    {
        return GetCenter() + new Vector3(rowcol.x, rowcol.y, 0);
    }

    public enum Direction { NONE, TOP_, BOTT, LEFT, RIGH };
    public Jewel NewJewel(List<Vector2Int> freeSpaces = null, Direction startingPoint = Direction.NONE) {
        if(freeSpaces == null) freeSpaces = GetFreeSpaces();
        if (freeSpaces.Count > 0)
        {
            int loc = Random.Range(0, freeSpaces.Count);
            int i = Random.Range(0, possibleJewels.Length);
            Jewel j = (Instantiate(possibleJewels[i].gameObject) as GameObject).GetComponent<Jewel>();
            j.transform.SetParent(transform);
            j.g = this;
            j.rowcol = freeSpaces[loc];
            switch (startingPoint)
            {
                case Direction.NONE: j.transform.position = j.IdealPosition(); break;
                case Direction.TOP_: j.transform.position = GetPosition(new Vector2Int(j.rowcol.x, size.y + j.rowcol.y)); break;
                case Direction.BOTT: j.transform.position = GetPosition(new Vector2Int(j.rowcol.x, -1 - j.rowcol.y)); break;
                case Direction.LEFT: j.transform.position = GetPosition(new Vector2Int(-1 - j.rowcol.x, j.rowcol.y)); break;
                case Direction.RIGH: j.transform.position = GetPosition(new Vector2Int(size.x + j.rowcol.x, j.rowcol.y)); break;
            }
            freeSpaces.RemoveAt(loc);
            board[j.rowcol.y, j.rowcol.x] = j;
            dirty = true;
            return j;
        }
        return null;
    }
    void Start()
    {
        board = new Jewel[size.y, size.x];
        int count = size.x * size.y;
        freeSpaces = GetFreeSpaces();
        ParticleState(false);
    }

    public int IsInList(Jewel j, List<List<Jewel>> list)
    {
        for(int i = 0; i < list.Count; ++i) { if (list[i].IndexOf(j) >= 0) return i; }
        return -1;
    }

    public List<List<Jewel>> CalculateReadyToActivate()
    {
    // TODO validate that every jewel is in the correct board slot (row/col matches!)
        List<List<Jewel>> vlist = new List<List<Jewel>>();
        List<List<Jewel>> hlist = new List<List<Jewel>>();
        for (int r = 0; r < size.y-2; ++r)
        {
            for (int c = 0; c < size.x-2; ++c)
            {
                //Debug.Log((size.y-2)+" "+(size.x-2));
                Jewel j = board[r, c];
                if(j != null)
                {
                    int inCol = 0, inRow = 0, idx;
                    // check in a line down
                    while ((idx=r+inCol+1) < board.GetLength(0) && board[idx, c] != null 
                        && IsInList(board[idx, c], vlist) < 0 && board[idx, c].name == j.name) { inCol++; }
                    while ((idx=c+inRow+1) < board.GetLength(1) && board[r, idx] != null
                        && IsInList(board[r, idx], hlist) < 0 && board[r, idx].name == j.name) { inRow++; }
                    if (inCol >= thisManyInARowToActivate)
                    {
                        List<Jewel> line = new List<Jewel>();
                        for(int i = 0; i < inCol; ++i) { line.Add(board[r + inCol, c]); }
                        vlist.Add(line);
                        Debug.Log("h");
                        freeSpaces = null; // DEBUG
                    }
                    if (inRow >= thisManyInARowToActivate)
                    {
                        List<Jewel> line = new List<Jewel>();
                        for (int i = 0; i < inRow; ++i) { line.Add(board[r, c + inRow]); }
                        hlist.Add(line);
                        Debug.Log("v");
                        freeSpaces = null; // DEBUG
                    }

                }
            }
        }
        vlist.AddRange(hlist);
        return vlist;
    }

    public void RemoveCollidingActivationsWithPreferenceToSize(List<List<Jewel>> list)
    {
        list.Sort((a, b) => { return a.Count > b.Count ? -1 : a.Count < b.Count ? 1 : 0; });
        for(int i = 1; i < list.Count; ++i)
        {
            for(int j = list.Count-1; j > i; --j)
            {
                for (int k = 0; k < list[i].Count; ++k) {
                    bool collides = list[j].Contains(list[i][k]);
                    if (collides)
                    {
                        list.RemoveAt(j);
                        --j;
                    }
                }
            }
        }
    }

    public Dictionary<Direction,System.Comparison<Vector2Int>> sorts = new Dictionary<Direction,System.Comparison<Vector2Int>>{
        {Direction.TOP_, (a, b) => { return a.y > b.y ? 1 : a.y < b.y ? -1 : 0; } },
        {Direction.BOTT, (a, b) => { return a.y < b.y ? 1 : a.y > b.y ? -1 : 0; } },
        {Direction.LEFT, (a, b) => { return a.x < b.x ? 1 : a.x > b.x ? -1 : 0; } },
        {Direction.RIGH, (a, b) => { return a.x > b.x ? 1 : a.x < b.x ? -1 : 0; } },
    };

    public void SortByDirection(Direction dir, List<Vector2Int> locs = null)
    {
        //switch (dir)
        //{
        //    case Direction.TOP_: locs.Sort((a, b) => { return a.y > b.y ? 1 : a.y < b.y ? -1 : 0; }); break;
        //    case Direction.BOTT: locs.Sort((a, b) => { return a.y < b.y ? 1 : a.y > b.y ? -1 : 0; }); break;
        //    case Direction.LEFT: locs.Sort((a, b) => { return a.x < b.x ? 1 : a.x > b.x ? -1 : 0; }); break;
        //    case Direction.RIGH: locs.Sort((a, b) => { return a.x > b.x ? 1 : a.x < b.x ? -1 : 0; }); break;
        //}
        locs.Sort(sorts[dir]);
    }

    public void Pull(Direction dir, List<Vector2Int> locs)
    {
        //if (locs == null) locs = GetFreeSpaces();
        //SortByDirection(dir, locs);
        //for(int i = 0; i < locs.Count; ++i)
        switch(dir) {
        case Direction.TOP_:
            for(int x=0; x < size.x; ++x) {
                for(int y=0; y <size.y; ++y) {
                    // if we found an empty spot
                    if(board[y,x] == null) {
                        // find the next jewel that should be in that spot
                        Jewel j = null;
                        for(int i=y+1; i<size.y; ++i) {
                            if(board[i,x] != null) { j = board[i,x]; break; }
                        }
                        if(j != null) { Swap(new Vector2Int(y,x), j.rowcol); } else { break; }
                    }
                }
            }
            break;
        default:
            throw new System.Exception("only support top right now");
        }
    }

    public List<Vector2Int> LocationsOfJewels(List<List<Jewel>> list)
    {
        List<Vector2Int> locs = new List<Vector2Int>();
        for(int i = 0; i < list.Count; ++i)
        {
            for(int j = 0; j < list[i].Count; ++j)
            {
                locs.Add(list[i][j].rowcol);
            }
        }
        return locs;
    }

    public void ForEach(List<List<Jewel>> list, System.Action<int, int> cell) {
        for (int i = 0; i < list.Count; ++i)
        {
            for (int j = 0; j < list[i].Count; ++j)
            {
                cell.Invoke(i,j);
            }
        }
    }

    public List<Jewel> ResolveCollision(Direction dir)
    {
        List<List<Jewel>> list = CalculateReadyToActivate();
        List<Jewel> resolved = null;
        if (list.Count > 0)
        {
            Debug.Log(list.Count);
            ForEach(list, (i,j)=> { if(list[i][j] == null) { throw new System.Exception("NULL IS BAD "+i+" "+j); } });
            resolved = new List<Jewel>();
            RemoveCollidingActivationsWithPreferenceToSize(list);
            ForEach(list, (i,j)=> { if(list[i][j] == null) { throw new System.Exception("NULL IS BAD!! "+i+" "+j); } });
            Vector3 oldP = ps.transform.position;
            for (int i = 0; i < list.Count; ++i)
            {
                for (int j = 0; j < list[i].Count; ++j)
                {
                    ps.transform.position = list[i][j].transform.position;
                    ps.Emit(10);
                    list[i][j].transform.localScale = Vector3.one / 2;
                    resolved.Add(list[i][j]);
                }
            }
            //List<Vector2Int> locs = LocationsOfJewels(list);
            //for(int i = 0; i < locs.Count; ++i){
            //    RemoveJewelAt(locs[i]);
            //}
            //Pull(dir, locs);
            ////freeSpaces = GetFreeSpaces();
            //Debug.Log("???");
        }
        return resolved;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(freeSpaces != null && freeSpaces.Count > 0)
        {
            NewJewel(freeSpaces);
        }
        if(dirty){
            ResolveCollision(Direction.TOP_);
            dirty = false;
        }
    }

    public float Distance(Jewel a, Jewel b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    public void Swap(Vector2Int a, Vector2Int b)
    {
        Jewel ja = board[a.y, a.x];
        Jewel jb = board[b.y, b.x];
        if(ja != null) ja.rowcol = b;
        if(jb != null) jb.rowcol = a;
        board[a.y, a.x] = jb;
        board[b.y, b.x] = ja;
        dirty = true;
    }

    public void RemoveJewelAt(Vector2Int p)
    {
        Jewel j = board[p.y, p.x];
        board[p.y, p.x] = null;
        Destroy(j);
    }

    public void SetSelected(Jewel j)
    {
    	if(selected != null) {
			selected.transform.localScale = Vector3.one;
		}
		selected = j;
    	if(selected != null) {
			selected.transform.localScale = Vector3.one * selectedScale;
		}
        if(selected == null) {
            ParticleState(false);
            ps.transform.SetParent(null);
        } else {
            ParticleState(true);
            ps.transform.position = selected.transform.position + Vector3.back;
            ps.transform.SetParent(selected.transform);
        }
    }

    public void ParticleState(bool on) {
        ParticleSystem.EmissionModule em = ps.emission;
        em.enabled = on;
    }

    public void Click(Jewel j) {
        if(selected != null) {
            if (Distance(j, selected) <= swapDistance)
            {
                Swap(j.rowcol, selected.rowcol);
                SetSelected(null);
            }
        } else {
            SetSelected(j);
        }
    }
}
