﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model3dItemController : MonoBehaviour//model3d的每一个儿子都要有
{
    public class Cost
    {
        public float cost;
        public int id;
        public Cost(float cost,int id)
        {
            this.cost = cost;
            this.id = id;
        }
    }
    public class CostComparer:IComparer<Cost>
    {
        public int Compare(Cost x,Cost y)
        {
            if (x.cost == y.cost && x.id == y.id)
                return 0;
            if (x.cost < y.cost||(x.cost==y.cost&&x.id<y.id))
                return 1;
            else
                return -1;
        }
    }
    private ObjManager objManager;
    private PanelStatusController panelStatusController;
    private float triangleMultiNum;//三角面片倍数
    private Mesh oldMesh;//最初的网格
    private bool haveNormals;
    private bool haveUv;

    private void Awake()
    {
        objManager = GameObject.Find("ObjManager").GetComponent<ObjManager>();
        panelStatusController = objManager.panelStatusController;
        gameObject.layer = Macro.MODEL3D_ITEM;//设置为model3dItem层
        gameObject.tag = Macro.UNSELECTED;//将tag设置为未选中
    }
    // Use this for initialization    
    void Start () {
        triangleMultiNum = 1;
        if (GetComponent<SkinnedMeshRenderer>()!=null)//有SkinnedMeshRenderer
        {
            var mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
            oldMesh = new Mesh();
            MeshACopyToMeshB(mesh, oldMesh);
            haveNormals = oldMesh.normals.Length != 0;
            haveUv = oldMesh.uv.Length != 0;
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
    private void MeshACopyToMeshB(Mesh a,Mesh b)//把mesh a赋值给 mesh b
    {
        b.Clear();
        b.vertices = (Vector3[])a.vertices.Clone();
        b.normals = (Vector3[])a.normals.Clone();
        b.uv = (Vector2[])a.uv.Clone();
        b.triangles = (int[])a.triangles.Clone();
    }
    public float GetTriangleMultiNum()//获得三角形倍数
    {
        return triangleMultiNum;
    }
    public void SetTriangleMultiNum(float v)//设置三角形倍数
    {        
        if (!GetComponent<ItemController>().trianglesEditable)//不可编辑triangles
            return;
        if (triangleMultiNum == v)
            return;
        triangleMultiNum = v;
        UpdateTriangles();
    }
    private void UpdateTriangles()//根据倍数重新计算三角面片
    {        
        var mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
        var vertices = mesh.vertices;
        if (triangleMultiNum == 1)//1倍直接把原来存好的三角形给它
        {
            MeshACopyToMeshB(oldMesh, mesh);
            return;
        }

        var verticesList = new List<Vector3>();
        var normalsList = new List<Vector3>();
        var uvList = new List<Vector2>();
        int triangleNum = (int)(oldMesh.triangles.Length * triangleMultiNum);
        triangleNum += 3-triangleNum % 3;
        Debug.Log("triangleNum: " + triangleNum);
        //var triangles = new int[triangleNum];        
        var trianglesList = new List<int>();
        foreach (var item in oldMesh.vertices)
        {
            verticesList.Add(item);
        }

        foreach (var item in oldMesh.normals)
        {
            normalsList.Add(item);
        }

        foreach (var item in oldMesh.uv)
        {
            uvList.Add(item);
        }


        var sd = new SortedDictionary<Cost,int[]>(new CostComparer());        
        int id = 0;//新加入sd的cost的id
        for (int i = 0; i < oldMesh.triangles.Length; i+=3)
        {
            int[] outTriangle;
            int[] triangle = new int[] { oldMesh.triangles[i], oldMesh.triangles[i + 1], oldMesh.triangles[i + 2] };
            float len = DivideTriangle(triangle,out outTriangle,verticesList);
            sd.Add(new Cost(len,++id),triangle);
        }

        while (sd.Count * 3 < triangleNum)
        {
            var e = sd.GetEnumerator();
            e.MoveNext();
            var ec = e.Current;
            int[] triangle = ec.Value;//取最长边最长的三角形进行划分      
            sd.Remove(ec.Key);//从sd中删掉该三角形
            int[] outTriangle;
            DivideTriangle(triangle, out outTriangle, verticesList);
            int tot = verticesList.Count;//新加的点的index
            verticesList.Add((verticesList[outTriangle[0]] + verticesList[outTriangle[1]]) / 2);//加入最长边中点
            var normal = new Vector3(0, 0, 0);
            var uv = new Vector2(0, 0);
            foreach (var i in triangle)
            {
                if (haveNormals)
                    normal += normalsList[i];
                if (haveUv)
                    uv += uvList[i];
            }
            normal /= triangle.Length;
            uv /= triangle.Length;
            if (haveNormals)
                normalsList.Add(normal);
            if (haveUv)
                uvList.Add(uv);
            var t1 = new int[] { outTriangle[0], tot, outTriangle[2] };
            var t2 = new int[] { outTriangle[1], outTriangle[2], tot };

            var len = DivideTriangle(t1, out outTriangle, verticesList);
            sd.Add(new Cost(len, ++id), t1);
            len = DivideTriangle(t2, out outTriangle, verticesList);
            sd.Add(new Cost(len, ++id), t2);
        }

        int cc = 0;
        var enumerator = sd.GetEnumerator();
        for (int i = 0; enumerator.MoveNext(); i++)
        {
            //Debug.Log("length: "+enumerator.Current.Key.cost);
            int[] triangle = enumerator.Current.Value;
            for (int j = 0; j < 3; j++) {
                //triangles[i * 3 + j] = triangle[j];
                trianglesList.Add(triangle[j]);
                cc++;
            }
        }
        mesh.Clear();
        mesh.vertices = verticesList.ToArray();
        if (haveNormals)
            mesh.normals = normalsList.ToArray();
        if (haveUv)
            mesh.uv = uvList.ToArray();
        mesh.triangles = trianglesList.ToArray();
        panelStatusController.UpdateTextTriangleNum();
    }
    private float DivideTriangle(int []triangle,out int []outTriangle,List<Vector3> vertices)//切割三角形
    {
        float re = -1;
        int p = 0;
        for (int i = 0; i < 3; i++)
        {
            int a = triangle[i];
            int b = triangle[(i + 1) % 3];
            var len = (vertices[b] - vertices[a]).magnitude;
            if ( len > re)
            {
                p = i;
                re = len;
            }
        }
        outTriangle = new int[] { triangle[p],triangle[(p+1)%3],triangle[3-p- (p + 1) % 3] };//最长边的两个点以及该边所对的点的标号
        return re;
    }
}
