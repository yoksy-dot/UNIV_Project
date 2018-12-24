using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.EventSystems;

public class MeshMaker : MonoBehaviour
{
    [Tooltip("メッシュを張るかどうか")]
    public bool flag = false;

    //手書きの頂点リスト
    public List<Vector3> pos_list = new List<Vector3>();
    private Vector3[] _poslist;
    private static int list_num = 1100;//最大頂点数

    //返還後の頂点リスト
    private List<Vector3> pos_conversion = new List<Vector3>();

    [SerializeField]
    private readonly float Interval = 0.1f;

    private float timer = 0;

    Vector3 mousePos;
    Vector3 objPos;
    Vector2 screen_vec2;

    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private LineRenderer lineRenderer2;
    [SerializeField]
    private GameObject lineRenderer3;

    private int forward_num = 0;

    //メッシュ作成変数
    Mesh mesh;
    //int[] tri = new int[list_num];
    List<int> tri = new List<int>();
    //int[] fake_tri = { 0, 1, 2, 0, 2, 3 };
    public Material material;

    //乖離度閾値
    private static float Threshold = 2.0f;

    //ファイルのパス
    private static string FilePath = "Assets/Script/VectorList.txt";

    private StringBuilder sb;
    private string myString;

    //トグル用変数
    private int toggle_num = 0;

    //手法2用_分割数
    private static int Split = 119;

    public GameObject maru;

    //デバッグ用座標配列
    public List<Vector3> test_list = new List<Vector3>();

    // Use this for initialization
    void Start()
    {
        mesh = new Mesh();

        lineRenderer.positionCount = 0;
        screen_vec2 = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

        sb = new StringBuilder();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        mousePos = Input.mousePosition;
        //Debug.Log(mousePos.x);

        if (/*timer > Interval &&*/ mousePos.x < 820 && Input.GetMouseButton(0) && pos_list.Count < list_num && !EventSystem.current.IsPointerOverGameObject())
        {
            //スクリーン座標変換

            mousePos.z = 10.0f;
            objPos = Camera.main.ScreenToWorldPoint(mousePos);
            //画面外に出ないように
            objPos.x = Mathf.Clamp(objPos.x, -screen_vec2.x, 6.5f);
            objPos.y = Mathf.Clamp(objPos.y, -screen_vec2.y, screen_vec2.y);
            //一定距離未満ならリストに入れない
            //if (pos_list.Count > 0 && (pos_list[pos_list.Count - 1] - objPos).sqrMagnitude <= 0.1f)
            //{
            //    return;
            //}
            //リストへ
            pos_list.Add(objPos);
            //インターバル初期化
            timer = 0;
            //ラインレンダラーの処理
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(forward_num, pos_list[forward_num++]);

        }
        else if (Input.GetMouseButtonUp(1))
        {
            //右クリックで一つ戻る
            pos_list.RemoveAt(pos_list.Count - 1);
            lineRenderer.positionCount--;
            forward_num--;
        }
    }

    public void Button_MakeMesh()
    {
        if (pos_list.Count < 2)
        {
            return;
        }
        //int num = 1;

        _poslist = pos_list.ToArray();

        //for (int i = 0; i < (pos_list.Count-2)*3; i++)
        //{

        //    if(i%3==0)
        //        tri.Add(0);
        //    else if (i % 3 == 1)
        //    {
        //        tri.Add(num);
        //    }
        //    else if (i % 3 == 2)
        //    {
        //        tri.Add(++num);
        //    }

        //}


        if (flag)
        {
            mesh.vertices = _poslist;
            mesh.triangles = tri.ToArray();
            mesh.RecalculateNormals();

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (!meshFilter) meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;
            meshRenderer.sharedMaterial = material;
        }

        //確認用
        //List<Vector3> sss = new List<Vector3>();
        //sss.Add(new Vector3(0f, 0));
        //sss.Add(new Vector3(120.0f, 0));
        //sss.Add(new Vector3(120.0f, 120.0f));
        //sss.Add(new Vector3(0f, 120.0f));

        switch (toggle_num)
        {
            case 0:
                Technique1(pos_list);
                break;
            case 1:
                Technique2(pos_list);
                break;
            case 2:
                Technique3(pos_list);
                break;
            case 3:
                //デバッグ用
                //DebugFunc();
                //pos_list = test_list;
                //TechniqueA(pos_list);

                if (pos_list.Count < 120)
                {
                    Technique3(pos_list);
                }
                else
                {
                    TechniqueA(pos_list);
                }
                break;
            case 4:
                TechniqueB(pos_list);
                break;
        }

    }

    public void Button_Clear()
    {
        //初期化ボタン
        pos_list.Clear();
        pos_conversion.Clear();
        lineRenderer.positionCount = 0;
        forward_num = 0;
        lineRenderer2.positionCount = 0;

        pos_conversion.Clear();



        foreach (Transform child in transform)//青線消去
        {
            if (child.name == "Line3(Clone)")
                Destroy(child.gameObject);

            if (child.name == "まる(Clone)")
                Destroy(child.gameObject);
        }

    }

    public void Button_MakeFile()
    {
        float fileCount = pos_conversion.Count;
        //float fileCount = pos_list.Count;

        //ファイル生成
        if (pos_list.Count < 1/* || pos_conversion.Count > 1*/)
            return;

        if (pos_conversion.Count % 2 != 0)
        {
            Debug.Log("座標を一つ減らしました");
            fileCount--;
        }


        sb.Append(fileCount);//最初の行は頂点数
        sb.Append("\n");
        myString += sb.ToString();
        sb.Length = 0;
        for (int i = 0; i < fileCount; i++)
        {
            sb.Append(pos_conversion[i].x);
            sb.Append(" ");
            sb.Append(pos_conversion[i].y);
            sb.Append("\n");
            myString += sb.ToString();
            sb.Length = 0;
        }

        ////デバッグ用座標とり
        //for (int i = 0; i < fileCount; i++)
        //{
        //    sb.Append(pos_list[i].x);
        //    sb.Append(" ");
        //    sb.Append(pos_list[i].y);
        //    sb.Append("\n");
        //    myString += sb.ToString();
        //    sb.Length = 0;
        //}

        File.WriteAllText(FilePath, myString);
    }

    public void ToggleFunc(int num)
    {
        toggle_num = num;
    }

    private void Technique1(List<Vector3> pos_list)
    {
        /*
         * 辺の長さの平均をとった後、すべての頂点が等間隔なるように変換を行う
         * 最初に考えたやつ
         * とりあえずタイリング可能
         */
        int i;
        float vec_lngth, heikin;
        List<Vector3> SideLength;//辺の長さを格納(正規化してない)

        vec_lngth = 0;
        heikin = 0;
        flag = false;
        SideLength = new List<Vector3>();

        for (i = 1; i < pos_list.Count; i++)//すべての辺の長さを計算
        {
            Vector3 vec = pos_list[i - 1] - pos_list[i];
            SideLength.Add(vec);
            vec_lngth += vec.magnitude;
        }
        //始点と終点の長さ
        Vector3 vec_s = pos_list[pos_list.Count - 1] - pos_list[0];
        SideLength.Add(vec_s);
        vec_lngth += vec_s.magnitude;

        heikin = vec_lngth / pos_list.Count;
        //Debug.Log("平均の長さは" + heikin + "です");

        //Debug.Log(SideLength.Count);
        for (i = 0; i < SideLength.Count; i++)//すべての辺の長さに対して平均の長さとの乖離度を算出
        {
            //
            if ((SideLength[i] / heikin).magnitude >= Threshold)//とりあえず2
            {
                //Debug.Log("side: " + SideLength.Count + " i: " + i);
                if (i != SideLength.Count - 1)
                {
                    pos_list.Insert(i + 1, pos_list[i] + SideLength[i].normalized * heikin);
                }
                else
                {
                    //Debug.Log("aaa");
                    pos_list.Insert(i + 1, pos_list[i] + vec_s.normalized * heikin);
                }
                SideLength.Insert(i, SideLength[i].normalized * heikin);
                SideLength[i + 1] -= SideLength[i];
            }

        }


        pos_conversion.Add(pos_list[0]);//スタートは同じかつ初期地点
        lineRenderer2.positionCount = 1;
        lineRenderer2.SetPosition(0, pos_conversion[0]);
        for (i = 1; i < pos_list.Count; i++)
        {
            Vector3 vec = pos_conversion[i - 1] - (SideLength[i - 1].normalized * heikin);
            pos_conversion.Add(vec);//

            //ラインレンダラーの処理
            lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(i, vec);
        }
        lineRenderer2.positionCount++;
        lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_list[0]);//閉じるための線

        //func2();
        //Debug.Log(yogenteiri(Vector3.zero, Vector3.up, Vector3.right));

    }

    private void Technique2(List<Vector3> pos_list)
    {
        /*
         先生の言ってた方法
         タイリングに失敗
         */
        int front_num = 0;//現在どこの頂点を参照にしているか

        float vec_lngth = 0, heikin = 0;
        List<Vector3> SideLength = new List<Vector3>();//辺の長さを格納(正規化してない)

        for (int i = 1; i < pos_list.Count; i++)//すべての辺の長さを計算
        {
            Vector3 vec = pos_list[i - 1] - pos_list[i];
            SideLength.Add(vec);
            vec_lngth += vec.magnitude;
        }
        //終点から始点の長さ
        Vector3 vec_s = pos_list[pos_list.Count - 1] - pos_list[0];
        SideLength.Add(vec_s);
        vec_lngth += vec_s.magnitude;

        pos_list.Add(pos_list[0]);

        heikin = vec_lngth / Split;

        pos_conversion.Add(pos_list[0]);//スタートは同じかつ初期地点
        lineRenderer2.positionCount = 1;
        lineRenderer2.SetPosition(0, pos_conversion[0]);

        for (int i = 0; i < Split; i++)
        {
            Vector3 vec = pos_conversion[i] - (SideLength[front_num].normalized * heikin);
            if ((vec - pos_list[front_num + 1]).sqrMagnitude > heikin * heikin)
            {
                pos_conversion.Add(vec);

                //ラインレンダラーの処理
                lineRenderer2.positionCount++;
                lineRenderer2.SetPosition(i + 1, vec);
            }
            else
            {
                //Debug.Log("平均"+heikin+"未満:"+(vec - pos_list[front_num + 1]).magnitude);
                if (front_num + 1 < pos_list.Count - 1)
                {
                    front_num++;
                }

                else
                    front_num = 0;

                pos_conversion.Add(pos_list[front_num]);

                //ラインレンダラーの処理
                lineRenderer2.positionCount++;
                lineRenderer2.SetPosition(i + 1, pos_list[front_num]);
            }
        }

        lineRenderer2.positionCount--;
        //lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_list[0]);//閉じるための線

        MarkFunc(pos_conversion);
    }

    private void Technique3(List<Vector3> pos_list)
    {
        /*
         1と2の合わせ技
         */

        int front_num = 0;//現在どこの頂点を参照にしているか
        //int bunnkatu = (pos_list.Count + 1) * 10;//変形後の頂点の分割数
        int bunnkatu = 100;

        float vec_lngth = 0, heikin = 0;
        List<Vector3> SideLength = new List<Vector3>();//辺の長さを格納(正規化してない)
        List<Vector3> SideLength2 = new List<Vector3>();//再変換のためのベクトルの向きを格納

        for (int i = 1; i < pos_list.Count; i++)//すべての辺の長さを計算
        {
            Vector3 vec = pos_list[i - 1] - pos_list[i];
            SideLength.Add(vec);
            vec_lngth += vec.magnitude;
        }
        //終点から始点の長さ
        Vector3 vec_s = pos_list[pos_list.Count - 1] - pos_list[0];
        SideLength.Add(vec_s);
        vec_lngth += vec_s.magnitude;

        pos_list.Add(pos_list[0]);//最後

        heikin = vec_lngth / bunnkatu;

        Debug.Log("L:" + vec_lngth + " 平均:" + heikin);

        pos_conversion.Add(pos_list[0]);//スタートは同じかつ初期地点
        lineRenderer2.positionCount = 1;
        lineRenderer2.SetPosition(0, pos_conversion[0]);


        Vector3 now_length = Vector3.zero;//元の辺のどこまで返還後の長さが来ているか

        //Debug.Log("長さの配列数:" + SideLength.Count);

        for (int i = 0; i < bunnkatu + 1; i++)
        {
            Vector3 vec_demi = SideLength[front_num].normalized * heikin;
            Vector3 vec = pos_conversion[i] - vec_demi;
            now_length += vec_demi;

            //Debug.Log("L:" + now_length.sqrMagnitude + " Side:" + SideLength[front_num].sqrMagnitude + " num:" + front_num);

            if (now_length.sqrMagnitude < (SideLength[front_num]).sqrMagnitude)
            {
                //Debug.Log("I:" + i);

            }
            else//超えたとき
            {
                now_length = Vector3.zero;
                //Debug.Log(i + ":こえてない？ " + front_num);
                if (front_num + 1 < SideLength.Count)
                {
                    front_num++;
                }
                else
                {
                    Debug.Log("0です  " + front_num);
                    front_num = 0;

                }


            }

            pos_conversion.Add(vec);

            //ラインレンダラーの処理
            lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(i + 1, vec);
        }
        //pos_covertionから始点までを追加でつないでいく
        Vector3 Last_vec = (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]);

        while (true)
        {

            if (!(Last_vec.sqrMagnitude > heikin * heikin)/* || p > 1*/)
            {
                break;
            }

            Debug.Log("hoge:" + pos_conversion.Count);
            pos_conversion.Add(pos_conversion[pos_conversion.Count - 1] - Last_vec.normalized * heikin);

            lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_conversion[pos_conversion.Count - 1]);

            Last_vec = (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]);

        }

        ////２点間から等間隔に頂点をつくる
        //Vector3 dds = (pos_conversion[0] - 3 * pos_conversion[pos_conversion.Count - 1]) / 2;

        //pos_conversion.Add(pos_conversion[pos_conversion.Count - 1] - dds.normalized * heikin);

        //lineRenderer2.positionCount++;
        //lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_conversion[pos_conversion.Count - 1]);

        Debug.Log("concertuion終点から始点までの距離" + (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]).magnitude);

        //lineRenderer2.positionCount--;

        //すべての処理が終わった後に全周と平均の長さを表示する
        float num_a = 0, num_b;
        for (int i = 1; i < pos_conversion.Count; i++)
        {
            Vector3 _vec = pos_conversion[i - 1] - pos_conversion[i];
            SideLength2.Add(_vec);
            num_a += _vec.magnitude;
        }
        num_a += (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]).magnitude;//全周

        num_b = num_a / pos_conversion.Count;//平均
        Debug.Log("辺の長さ:" + num_a + "平均値:" + num_b);

        //出した平均値でもう一回変換を行う
        for (int i = 1; i < pos_conversion.Count - 1; i++)
        {
            //Vector3 hoge_vec = pos_conversion[i - 1] - pos_conversion[i];
            pos_conversion[i] = pos_conversion[i - 1] - SideLength2[i].normalized * num_b;

            lineRenderer2.SetPosition(i, pos_conversion[i]);

        }

        MarkFunc(pos_conversion);
    }

    private void TechniqueA(List<Vector3> pos_list)
    {
        float vec_lngth = 0, heikin = 0;
        float L = 0;
        //int front_num = 0;
        float exp_num = 1;

        List<Vector3> SideLength = new List<Vector3>();//辺の長さを格納(正規化してない)
        //List<Vector3> SideLength2 = new List<Vector3>();//再変換のためのベクトルの向きを格納

        //List<Vector3> pos_conversion2 = new List<Vector3>();//

        List<Vector3> kari_pos = new List<Vector3>(pos_list);//pos_listをコピー
        Vector3 _vec = Vector3.zero;

        //while (true)
        //{
        SideLength.Clear();
        for (int i = 1; i < kari_pos.Count; i++)//すべての辺の長さを計算
        {
            Vector3 vec = kari_pos[i - 1] - kari_pos[i];
            SideLength.Add(vec);
            vec_lngth += vec.magnitude;
        }
        //終点から始点の長さ
        Vector3 vec_s = kari_pos[kari_pos.Count - 1] - kari_pos[0];
        //SideLength.Add(vec_s);
        vec_lngth += vec_s.magnitude;

        //kari_pos.Add(kari_pos[0]);//最後

        heikin = vec_lngth / 120;


        //int kkkk = 0;
        ////蓋作成
        //while (true)
        //{
        //    kkkk++;
        //    Debug.Log("qwe " + (vec_s.sqrMagnitude));
        //    if (vec_s.sqrMagnitude < heikin || kkkk > 50)//後で調整 少し怪しい+時々無限ループになっている模様
        //    {
        //        break;
        //    }
        //    Vector3 a = kari_pos[kari_pos.Count - 1] - (vec_s.normalized * heikin);
        //    kari_pos.Add(a);
        //    //SideLength.Add(kari_pos[kari_pos.Count - 2] - kari_pos[kari_pos.Count - 1]);
        //    SideLength.Add(vec_s.normalized * heikin);
        //    //lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, kari_pos[kari_pos.Count - 1]);

        //    vec_s = (kari_pos[kari_pos.Count - 1] - pos_list[0]);
        //}
        

        Debug.Log("L:" + vec_lngth + " 平均:" + heikin);

        pos_conversion.Add(kari_pos[0]);//スタートは同じかつ初期地点
        lineRenderer2.positionCount = 1;
        lineRenderer2.SetPosition(0, pos_conversion[0]);

        //if (kari_pos.Count > 600 && kari_pos.Count < 800)
        //    exp_num = 1.9f;
        //else if (kari_pos.Count >= 800 && kari_pos.Count < 1200)
        //    exp_num = 4;
        //else
        //    exp_num = 1;

        if (kari_pos.Count >= 900 && kari_pos.Count < 1200)
            exp_num = 4f;
        else if (kari_pos.Count >= 700 && kari_pos.Count < 900)
            exp_num = 2.6f;
        else if (kari_pos.Count >= 600 && kari_pos.Count < 700)
            exp_num = 2.0f;
        else if (kari_pos.Count >= 500 && kari_pos.Count < 600)
            exp_num = 1.4f;
        else 
            exp_num = 1;


        for (int i = 0; i < SideLength.Count; i++)
        {
            _vec += SideLength[i];
            L += _vec.magnitude;
            if (L >= exp_num * heikin)
            {
                Vector3 a = pos_conversion[pos_conversion.Count - 1] - (_vec.normalized.normalized * (heikin / 2));
                pos_conversion.Add(a);
                //vec_lngth += a.magnitude;//平均の再計算

                //front_num++;
                L = 0;
                _vec = Vector3.zero;
            }
        }


        //pos_covertionから始点までを追加でつないでいく
        Vector3 Last_vec = (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]);

        while (true)
        {

            if (!(Last_vec.sqrMagnitude > heikin * heikin * 0.25)/* || p > 1*/)
            {
                break;
            }

            //Debug.Log("hoge:" + pos_conversion.Count);
            pos_conversion.Add(pos_conversion[pos_conversion.Count - 1] - Last_vec.normalized * (heikin / 2));

            //lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_conversion[pos_conversion.Count - 1]);

            Last_vec = (pos_conversion[pos_conversion.Count - 1] - pos_conversion[0]);

        }

        //線引き
        for (int i = 0; i < pos_conversion.Count - 1; i++)
        {
            //ラインレンダラーの処理
            lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(i, pos_conversion[i]);
        }
        lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, pos_conversion[0]);//ラスト

        //頂点配置
        MarkFunc(pos_conversion);
    }

    private void TechniqueB(List<Vector3> pos_list)
    {
        List<Vector3> kari_pos = new List<Vector3>(pos_list);//pos_listを一時保存
        List<Vector3> SideLength = new List<Vector3>();//辺の長さを格納(正規化してない)
        List<int> point_num = new List<int>();//特徴点の頂点番号保存
        float vec_lngth = 0, heikin = 0;
        int front_num = 0;

        for (int i = 1; i < kari_pos.Count; i++)//すべての辺の長さを計算
        {
            Vector3 vec = kari_pos[i - 1] - kari_pos[i];
            SideLength.Add(vec);
            vec_lngth += vec.magnitude;
            if (vec.sqrMagnitude < 0.01f)
            {
                kari_pos.Remove(kari_pos[i]);
                i--;
            }
        }
        //終点から始点の長さ
        Vector3 vec_s = kari_pos[kari_pos.Count - 1] - kari_pos[0];
        SideLength.Add(vec_s);
        vec_lngth += vec_s.magnitude;

        kari_pos.Add(kari_pos[0]);//最後

        heikin = vec_lngth / 120;//一辺の長さ
        Debug.Log(heikin);
        //pos_conversion = pos_list;
        func2(kari_pos,point_num);
        pos_conversion.Add(pos_list[0]);//始点

        if(point_num.Count < 1)
        {
            Debug.Log("鋭角が見当たりませんでした");
            return;
        }
        int side_point = Mathf.FloorToInt((120 - point_num.Count) / point_num.Count);//一辺に含まれる頂点数
        int farst = Mathf.FloorToInt(side_point / 2);//最初と最後のための頂点数
        float imamade = 0;//前の頂点から進んだ値

        for (int j = 0; j < point_num.Count; j++)
        {
            for (int i = 1; i < side_point+1; i++)
            {
                Debug.Log("ddd");
                //if (j == 1)
                //{
                //    i += farst;
                //}

                float v1 = (kari_pos[i-1] - kari_pos[i]).magnitude;

                if (heikin < imamade + v1)
                {
                    imamade += v1;
                }
                else
                {
                    pos_conversion.Add((kari_pos[i - 1] - kari_pos[i]).normalized * (v1 - heikin));
                    
                }
            }
        }


        for (int i = 0; i < pos_conversion.Count; i++)
        {
            lineRenderer2.positionCount++;
            lineRenderer2.SetPosition(i, pos_conversion[i]);
        }

        
        
        MarkFunc(pos_conversion);
    }

    private void func2(List<Vector3> vec,List<int> a)
    {
        /*
         * 鋭角検知
         * 
         */
        float dig = 0;//cosの値が入る

        for (int i = 0; i < vec.Count - 2; i++)
        {
            dig = yogenteiri(vec[i], vec[i + 1], vec[i + 2]);
            if ((dig <= 1 && dig >= 0))
            {
                //Debug.Log("LOOOG");
                LineRenderer _line;
                //digが鋭角なら
                GameObject obj = Instantiate(lineRenderer3, transform);
                obj.tag = "Target";
                _line = obj.GetComponent<LineRenderer>();
                _line.SetPosition(0, vec[i]);
                _line.SetPosition(1, vec[i + 1]);
                _line.SetPosition(2, vec[i + 2]);

                a.Add(i+1);//尖っている頂点(真ん中)をadd
            }
        }

    }

    //計算式合ってる
    private float yogenteiri(Vector3 a, Vector3 b, Vector3 c)
    {
        float ab = (a - b).magnitude;
        float bc = (b - c).magnitude;
        float ac = (a - c).sqrMagnitude;
        float ans = (ac - (ab * ab) - (bc * bc)) / (-2 * ab * bc);

        //Debug.Log("aaa" + ans);
        return ans;
    }

    private void MarkFunc(List<Vector3> _vec)
    {
        for (int i = 0; i < _vec.Count - 1; i++)
        {
            GameObject obj = Instantiate(maru, _vec[i], Quaternion.identity);
            obj.transform.parent = transform;
        }
    }

    private void DebugFunc()
    {
        Debug.Log("A");
        TextAsset textAsset = new TextAsset();
        string[] textMessage; //テキストの加工前の一行を入れる変数
        string[] textnumeber = new string[2];

        textAsset = Resources.Load("Test", typeof(TextAsset)) as TextAsset;
        string TextLines = textAsset.text; //テキスト全体をstring型で入れる変数を用意して入れる

        //Splitで一行づつを代入した1次配列を作成
        textMessage = TextLines.Split('\n');
        Debug.Log("B");
        foreach (string a in textMessage)
        {
            textnumeber = a.Split(' ');

            test_list.Add(new Vector3(float.Parse(textnumeber[0]), float.Parse(textnumeber[1]) ));
        }
        Debug.Log("C");
    }
}
