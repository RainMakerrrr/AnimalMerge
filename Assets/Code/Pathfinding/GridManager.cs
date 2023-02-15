using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Pathfinding
{
#if UNITY_EDITOR
    [CustomEditor(typeof(GridManager)), CanEditMultipleObjects]
    class GridManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GridManager gmS = (GridManager) target;
            if (GUILayout.Button("Make Grid"))
                gmS.MakeGrid();
            if (GUILayout.Button("Make Circle"))
                gmS.MakeCircle();

            DrawDefaultInspector();
        }
    }
#endif


    public class GridManager : MonoBehaviour
    {
        public FindPathType find_path;
        public Vector2 v2_grid;
        public RectTransform rt;
        public GridLayoutGroup glg;
        public GameObject go_pref_tile;
        public GridObject char_s;
        public GridObject char_s2;
        public List<GridTile> db_tiles;
        public List<int> db_direction_order;


        void Update()
        {
            // if (Input.GetMouseButtonDown(1) && !char_s.moving && char_s.tile_s != char_s.selected_tile_s &&
            //     char_s.selected_tile_s != null)
            // {
            //     if (find_path == FindPathType.OnClick)
            //         FindPathsRealtime(char_s, char_s.selected_tile_s);
            //
            //     if (char_s.selected_tile_s.db_path_lowest.Count > 0)
            //         char_s.MoveToTile(char_s.selected_tile_s);
            //     else
            //         print("no valid tile selected");
            // }
        }


        //**Once Per Turn/Max Tile Pathfinding**//
        public void FindPathsStatic(GridObject tchar)
        {
            var ttile = tchar.tile_s;
            for (int x = 0; x < db_tiles.Count; x++)
                db_tiles[x].db_path_lowest.Clear(); //Clear all previous lowest paths for this char//

            List<GridTile> db_tpath = new List<GridTile>();
            FindNextPathStatic(tchar, ttile, db_tpath);
        }


        void FindNextPathStatic(GridObject tchar, GridTile ttile, List<GridTile> db_tpath)
        {
            for (int x = 0; x < ttile.db_neighbors.Count; x++)
            {
                var ntile = ttile.db_neighbors[x].tile_s;
                if (ttile.db_neighbors[x].tile_s != null && !db_tpath.Contains(ntile) &&
                    !ttile.db_neighbors[x].blocked &&
                    ntile.db_chars.Count == 0) //Check if tile, if not already used, if not blocked//
                {
                    if (find_path == FindPathType.OncePerTurn || (db_tpath.Count < tchar.max_tiles))
                    {
                        if (ntile.db_path_lowest.Count == 0 || db_tpath.Count + 1 < ntile.db_path_lowest.Count)
                        {
                            if ((!tchar.big) || (tchar.big && ntile.db_neighbors[1].tile_s != null &&
                                                 !ntile.db_neighbors[1].blocked &&
                                                 ntile.db_neighbors[2].tile_s != null &&
                                                 !ntile.db_neighbors[2].blocked &&
                                                 ntile.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null &&
                                                 !ntile.db_neighbors[1].tile_s.db_neighbors[2].blocked &&
                                                 !ntile.db_neighbors[2].tile_s.db_neighbors[1].blocked))
                            {
                                ntile.db_path_lowest.Clear();
                                for (int i = 0; i < db_tpath.Count; i++)
                                    ntile.db_path_lowest.Add(db_tpath[i]);

                                if (find_path == FindPathType.MaxTiles)
                                    ntile.im.color = Color.blue;

                                ntile.db_path_lowest.Add(ntile);
                                FindNextPathStatic(tchar, ntile, ntile.db_path_lowest);
                            }
                        }
                    }
                }
            }
        }


        //**On_hover/On_Click Pathfinding**//
        public void FindPathsRealtime(GridObject tchar, GridTile tar_tile_s)
        {
            var ttile = tchar.tile_s;
            for (int x = 0; x < db_tiles.Count; x++)
                db_tiles[x].db_path_lowest.Clear(); //Clear all previous lowest paths for this char//

            int up = (int) ttile.v2xy.x - (int) tar_tile_s.v2xy.x;
            int right = (int) tar_tile_s.v2xy.y - (int) ttile.v2xy.y;
            int down = (int) tar_tile_s.v2xy.x - (int) ttile.v2xy.x;
            int left = (int) ttile.v2xy.y - (int) tar_tile_s.v2xy.y;

            db_direction_order.Clear();
            if (up >= right && up >= down && up >= left)
            {
                db_direction_order.Add(0);
                db_direction_order.Add(1);
                db_direction_order.Add(2);
                db_direction_order.Add(3);
            }
            else if (right >= up && right >= down && right >= left)
            {
                db_direction_order.Add(1);
                db_direction_order.Add(2);
                db_direction_order.Add(3);
                db_direction_order.Add(0);
            }
            else if (down >= up && down >= right && down >= left)
            {
                db_direction_order.Add(2);
                db_direction_order.Add(3);
                db_direction_order.Add(0);
                db_direction_order.Add(1);
            }
            else
                //if (left >= up && left >= right && left >= down)
            {
                db_direction_order.Add(3);
                db_direction_order.Add(0);
                db_direction_order.Add(1);
                db_direction_order.Add(2);
            }

            List<GridTile> db_tpath = new List<GridTile>();
            FindNextPathRealtime(tchar, ttile, db_tpath, tar_tile_s);
        }


        void FindNextPathRealtime(GridObject tchar, GridTile ttile, List<GridTile> db_tpath, GridTile tar_tile_s)
        {
            for (int x = 0; x < ttile.db_neighbors.Count; x++)
            {
                var donum = db_direction_order[x];
                var ntile = ttile.db_neighbors[donum].tile_s;
                if (ttile.db_neighbors[donum].tile_s != null && !db_tpath.Contains(ntile) &&
                    !ttile.db_neighbors[donum].blocked  &&
                    ntile.db_chars.Count == 0) //Check if tile, if not already used, if not blocked//
                {
                    if (tar_tile_s.db_path_lowest.Count == 0 || db_tpath.Count < tar_tile_s.db_path_lowest.Count)
                    {
                        if (ntile.db_path_lowest.Count == 0 || db_tpath.Count + 1 < ntile.db_path_lowest.Count)
                        {
                            if ((!tchar.big) || (tchar.big && ntile.db_neighbors[1].tile_s != null &&
                                                 !ntile.db_neighbors[1].blocked &&
                                                 ntile.db_neighbors[2].tile_s != null &&
                                                 !ntile.db_neighbors[2].blocked &&
                                                 ntile.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null &&
                                                 !ntile.db_neighbors[1].tile_s.db_neighbors[2].blocked &&
                                                 !ntile.db_neighbors[2].tile_s.db_neighbors[1].blocked))
                            {
                                ntile.db_path_lowest.Clear();
                                for (int i = 0; i < db_tpath.Count; i++)
                                    ntile.db_path_lowest.Add(db_tpath[i]);

                                ntile.db_path_lowest.Add(ntile);

                                if (ttile != tar_tile_s)
                                    FindNextPathRealtime(tchar, ntile, ntile.db_path_lowest, tar_tile_s);
                            }
                        }
                    }
                }
            }
        }


        public void HoverTile(GridTile ttile)
        {
            for (int x = 0; x < db_tiles.Count; x++)
            {
                if (find_path == FindPathType.MaxTiles && db_tiles[x].db_path_lowest.Count > 0)
                    db_tiles[x].im.color = Color.blue;
                else
                    db_tiles[x].im.color = new Color(1, 1, 1, 0);
            }

            if (!char_s.big)
            {
                char_s.selected_tile_s = ttile;
                ttile.im.color = char_s.col;
            }
            else if (char_s.big)
            {
                char_s.selected_tile_s = ttile;
                if (ttile.db_neighbors[2].tile_s != null && ttile.db_neighbors[1].tile_s != null &&
                    ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null && !ttile.db_neighbors[2].blocked &&
                    !ttile.db_neighbors[1].blocked && !ttile.db_neighbors[1].tile_s.db_neighbors[2].blocked &&
                    !ttile.db_neighbors[2].tile_s.db_neighbors[1].blocked)
                {
                    ttile.im.color = char_s.col;
                    ttile.db_neighbors[1].tile_s.im.color = char_s.col;
                    ttile.db_neighbors[2].tile_s.im.color = char_s.col;
                    ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s.im.color = char_s.col;
                }
                else
                    //Make the 4 tiles (if they exit) red to show not useable//
                {
                    ttile.im.color = Color.red;
                    if (ttile.db_neighbors[1].tile_s != null)
                    {
                        ttile.db_neighbors[1].tile_s.im.color = Color.red;
                        if (ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null)
                            ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s.im.color = Color.red;
                    }

                    if (ttile.db_neighbors[2].tile_s != null)
                        ttile.db_neighbors[2].tile_s.im.color = Color.red;
                }
            }

            if (!char_s.moving)
            {
                //**For pathfinding in real time, this becomes slower exponentially as the grid gets bigger, not recommended for anything above 100 tiles**//
                if (find_path == FindPathType.OnHover)
                    FindPathsRealtime(char_s, ttile);

                if (!char_s.big)
                {
                    char_s.tile_s.im.color = char_s.col;

                    if (find_path != FindPathType.OnClick)
                    {
                        for (int x = 0; x < char_s.selected_tile_s.db_path_lowest.Count; x++)
                            char_s.selected_tile_s.db_path_lowest[x].im.color = char_s.col;
                    }

                    if (ttile.db_path_lowest.Count == 0 && find_path != FindPathType.OnClick)
                        ttile.im.color = Color.red;
                }
                else if (char_s.big)
                {
                    //Char's tile(s) turn to team color as part of path//
                    if (char_s.selected_tile_s.db_path_lowest.Count > 0)
                    {
                        char_s.tile_s.im.color = char_s.col;
                        if (char_s.tile_s.db_neighbors[1].tile_s != null)
                        {
                            char_s.tile_s.db_neighbors[1].tile_s.im.color = char_s.col;
                            if (char_s.tile_s.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null)
                                char_s.tile_s.db_neighbors[1].tile_s.db_neighbors[2].tile_s.im.color = char_s.col;
                        }

                        if (char_s.tile_s.db_neighbors[2].tile_s != null)
                            char_s.tile_s.db_neighbors[2].tile_s.im.color = char_s.col;

                        //Path tiles turn to team color//
                        if (find_path != FindPathType.OnClick)
                        {
                            for (int x = 0; x < char_s.selected_tile_s.db_path_lowest.Count; x++)
                            {
                                char_s.selected_tile_s.db_path_lowest[x].im.color = char_s.col;
                                if (char_s.selected_tile_s.db_path_lowest[x].db_neighbors[1].tile_s != null)
                                {
                                    char_s.selected_tile_s.db_path_lowest[x].db_neighbors[1].tile_s.im.color =
                                        char_s.col;
                                    if (char_s.selected_tile_s.db_path_lowest[x].db_neighbors[1].tile_s.db_neighbors[2]
                                            .tile_s != null)
                                        char_s.selected_tile_s.db_path_lowest[x].db_neighbors[1].tile_s.db_neighbors[2]
                                            .tile_s.im.color = char_s.col;
                                }

                                if (char_s.selected_tile_s.db_path_lowest[x].db_neighbors[2].tile_s != null)
                                    char_s.selected_tile_s.db_path_lowest[x].db_neighbors[2].tile_s.im.color =
                                        char_s.col;
                            }
                        }
                    }
                    else
                        //No path, make tar tile(s) red//
                    if (find_path != FindPathType.OnClick)
                    {
                        ttile.im.color = Color.red;
                        if (ttile.db_neighbors[1].tile_s != null)
                        {
                            ttile.db_neighbors[1].tile_s.im.color = Color.red;
                            if (ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s != null)
                                ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s.im.color = Color.red;
                        }

                        if (ttile.db_neighbors[2].tile_s != null)
                            ttile.db_neighbors[2].tile_s.im.color = Color.red;
                    }
                }
            }
        }


        public void MakeGrid()
        {
            glg.enabled = true;

            //Clear Old Tiles//
            for (int i = 0; i < db_tiles.Count; i++)
                DestroyImmediate(db_tiles[i].gameObject);
            db_tiles.Clear();

            float twidth = (glg.cellSize.y + glg.spacing.y) * v2_grid.y;
            rt.sizeDelta = new Vector2(twidth, glg.cellSize.x);

            for (int x = 0; x < v2_grid.x; x++)
            {
                for (int y = 0; y < v2_grid.y; y++)
                {
                    var tgo = (GameObject) Instantiate(go_pref_tile, go_pref_tile.transform.position,
                        go_pref_tile.transform.rotation, go_pref_tile.transform.parent);
                    tgo.SetActive(true);
                    tgo.name = "tile_" + x + "_" + y;
                    var ttile = tgo.GetComponent<GridTile>();
                    ttile.v2xy = new Vector2Int(x, y);
                    db_tiles.Add(ttile);
                }
            }

            for (int x = 0; x < db_tiles.Count; x++)
            {
                for (int y = 0; y < db_tiles.Count; y++)
                {
                    if (db_tiles[x].v2xy.x - db_tiles[y].v2xy.x == 1 && db_tiles[x].v2xy.y == db_tiles[y].v2xy.y)
                        db_tiles[x].db_neighbors[0].tile_s = db_tiles[y]; //Up//
                    else if (db_tiles[x].v2xy.x == db_tiles[y].v2xy.x && db_tiles[y].v2xy.y - db_tiles[x].v2xy.y == 1)
                        db_tiles[x].db_neighbors[1].tile_s = db_tiles[y]; //Right//
                    else if (db_tiles[y].v2xy.x - db_tiles[x].v2xy.x == 1 && db_tiles[x].v2xy.y == db_tiles[y].v2xy.y)
                        db_tiles[x].db_neighbors[2].tile_s = db_tiles[y]; //Down//
                    else if (db_tiles[x].v2xy.x == db_tiles[y].v2xy.x && db_tiles[x].v2xy.y > db_tiles[y].v2xy.y)
                        db_tiles[x].db_neighbors[3].tile_s = db_tiles[y]; //Left//
                }
            }
        }


        public void MakeCircle()
        {
            glg.enabled = false;

            var pos_mid = db_tiles[0].transform.position + db_tiles[db_tiles.Count - 1].transform.position;
            pos_mid /= 2;
            var max_dist = Vector3.Distance(pos_mid, db_tiles[0].transform.position);
            var circle_dist = max_dist * 0.68f;

            var tcount = db_tiles.Count;
            for (int i = tcount - 1; i > -1; i--)
            {
                var ttile = db_tiles[i];
                var tdist = Vector3.Distance(pos_mid, ttile.transform.position);

                if (tdist > circle_dist)
                {
                    db_tiles.Remove(ttile);
                    DestroyImmediate(ttile.gameObject);
                }
            }
        }


        IEnumerator StartGame()
        {
            yield return new WaitForSeconds(0.01f);

            //var ttile = db_tiles[Random.Range(0, db_tiles.Count - 1)];
            var ttile = db_tiles.FirstOrDefault(tile => tile.name == "tile_0_0");
            char_s.tile_s = ttile;
            ttile.db_chars.Add(char_s);
            var tpos = ttile.transform.position;

            if (char_s.big)
            {
                tpos = new Vector3(0, 0, 0);
                tpos += ttile.transform.position + ttile.db_neighbors[1].tile_s.transform.position +
                        ttile.db_neighbors[2].tile_s.transform.position +
                        ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s.transform.position;
                tpos /= 4;
            }

            char_s.transform.position = tpos;

            if (find_path == FindPathType.OncePerTurn || find_path == FindPathType.MaxTiles)
                FindPathsStatic(char_s);
        }


        IEnumerator StartGame2()
        {
            yield return new WaitForSeconds(0.01f);

            //var ttile = db_tiles[Random.Range(0, db_tiles.Count - 1)];
            var ttile = db_tiles.FirstOrDefault(tile => tile.name == "tile_0_4");
            char_s2.tile_s = ttile;
            ttile.db_chars.Add(char_s2);
            var tpos = ttile.transform.position;

            if (char_s2.big)
            {
                tpos = new Vector3(0, 0, 0);
                tpos += ttile.transform.position + ttile.db_neighbors[1].tile_s.transform.position +
                        ttile.db_neighbors[2].tile_s.transform.position +
                        ttile.db_neighbors[1].tile_s.db_neighbors[2].tile_s.transform.position;
                tpos /= 4;
            }

            char_s2.transform.position = tpos;

            if (find_path == FindPathType.OncePerTurn || find_path == FindPathType.MaxTiles)
                FindPathsStatic(char_s);
        }


        void Start()
        {
            Debug.Log(db_tiles.Count);
            char_s.tile_s =
                db_tiles
                    [0]; //Slight delay in start game, this gives the char a tile so we don't get an onhover error during that milisecond//
            StartCoroutine(StartGame());
            StartCoroutine(StartGame2());
        }
    }
}