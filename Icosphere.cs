//Generated Icosphere in Godot/C#
//Adapted from the Unity Wiki
//http://wiki.unity3d.com/index.php/CreateIcoSphere

using Godot;
using System.Collections.Generic;
//I normally use System.Collections for everything, but ArrayMesh expects a Godot.Collections.Array
using Godot.Collections;

//run in editor
[Tool]
public class Icosphere : MeshInstance
{
    //use a property to regen in editor if changed
    private int _subdivisions = 0;
    [Export(PropertyHint.Range, "0,7,1")]
    public int Subdivisions
    {
        get { return _subdivisions; }
        set { _subdivisions = value; Generate(); }
    }

    protected struct TriangleIndices
    {
        public int v1;
        public int v2;
        public int v3;

        public TriangleIndices(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    List<Vector3> verts = new List<Vector3>();
    List<int> indices = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    protected int index = 0;
    protected List<TriangleIndices> faces = new List<TriangleIndices>();
    System.Collections.Generic.Dictionary<long, int> middlePointIndexCache =
        new System.Collections.Generic.Dictionary<long, int>();
    protected int AddVertex(Vector3 p)
    {
        float len = Mathf.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z);
        verts.Add(new Vector3(p.x / len, p.y / len, p.z / len));
        return index++;
    }

    public ArrayMesh CreateTheMesh()
    {
        Array arr = new Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        arr[(int)Mesh.ArrayType.Index] = indices.ToArray();
        arr[(int)Mesh.ArrayType.TexUv] = uvs.ToArray();
        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
        return arrayMesh;
    }

    public void CreateIcosphere()
    {
        // create 12 vertices of a icosahedron
        var t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        AddVertex(new Vector3(-1, t, 0));
        AddVertex(new Vector3(1, t, 0));
        AddVertex(new Vector3(-1, -t, 0));
        AddVertex(new Vector3(1, -t, 0));

        AddVertex(new Vector3(0, -1, t));
        AddVertex(new Vector3(0, 1, t));
        AddVertex(new Vector3(0, -1, -t));
        AddVertex(new Vector3(0, 1, -t));

        AddVertex(new Vector3(t, 0, -1));
        AddVertex(new Vector3(t, 0, 1));
        AddVertex(new Vector3(-t, 0, -1));
        AddVertex(new Vector3(-t, 0, 1));

        // create 20 triangles of the icosahedron
        faces = new List<TriangleIndices>();

        // 5 faces around point 0
        faces.Add(new TriangleIndices(0, 11, 5));
        faces.Add(new TriangleIndices(0, 5, 1));
        faces.Add(new TriangleIndices(0, 1, 7));
        faces.Add(new TriangleIndices(0, 7, 10));
        faces.Add(new TriangleIndices(0, 10, 11));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(1, 5, 9));
        faces.Add(new TriangleIndices(5, 11, 4));
        faces.Add(new TriangleIndices(11, 10, 2));
        faces.Add(new TriangleIndices(10, 7, 6));
        faces.Add(new TriangleIndices(7, 1, 8));

        // 5 faces around point 3
        faces.Add(new TriangleIndices(3, 9, 4));
        faces.Add(new TriangleIndices(3, 4, 2));
        faces.Add(new TriangleIndices(3, 2, 6));
        faces.Add(new TriangleIndices(3, 6, 8));
        faces.Add(new TriangleIndices(3, 8, 9));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(4, 9, 5));
        faces.Add(new TriangleIndices(2, 4, 11));
        faces.Add(new TriangleIndices(6, 2, 10));
        faces.Add(new TriangleIndices(8, 6, 7));
        faces.Add(new TriangleIndices(9, 8, 1));

        Subdivide(Subdivisions);
        // done, now add triangles to mesh
        foreach (var tri in faces)
        {
            indices.Add(tri.v3);
            indices.Add(tri.v2);
            indices.Add(tri.v1);
        }

        List<Vector2> UVs = new List<Vector2>();
        foreach (var v in verts)
        {
            var unitVector = v.Normalized();
            var ICOuv = Vector2.Zero;
            ICOuv.x = (Mathf.Atan2(unitVector.x, unitVector.z) + Mathf.Pi) / Mathf.Pi / 2;
            ICOuv.y = (Mathf.Acos(unitVector.y) + Mathf.Pi) / Mathf.Pi - 1;
            UVs.Add(new Vector2(ICOuv.x, ICOuv.y));
        }
        uvs = UVs;
    }

    public void Subdivide(int times = 1)
    {
        for (int i = 0; i < times; i++)
        {
            var faces2 = new List<TriangleIndices>();
            foreach (var tri in faces)
            {
                // replace triangle by 4 triangles
                int a = GetMiddlePoint(tri.v1, tri.v2);
                int b = GetMiddlePoint(tri.v2, tri.v3);
                int c = GetMiddlePoint(tri.v3, tri.v1);

                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }
    }

    private int GetMiddlePoint(int p1, int p2)
    {
        // first check if we have it already
        bool firstIsSmaller = p1 < p2;
        long smallerIndex = firstIsSmaller ? p1 : p2;
        long greaterIndex = firstIsSmaller ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;

        int ret;
        if (this.middlePointIndexCache.TryGetValue(key, out ret))
        {
            return ret;
        }

        // not in cache, calculate it
        Vector3 point1 = verts[p1];
        Vector3 point2 = verts[p2];

        Vector3 middle = new Vector3(
            (point1.x + point2.x) / 2.0f,
            (point1.y + point2.y) / 2.0f,
            (point1.z + point2.z) / 2.0f);

        // add vertex makes sure point is on unit sphere
        int i = AddVertex(middle);

        // store it, return index
        this.middlePointIndexCache.Add(key, i);
        return i;
    }

    public void Generate()
    {
        verts = new List<Vector3>();
        indices = new List<int>();
        index = 0;
        faces = new List<TriangleIndices>();
        middlePointIndexCache = new System.Collections.Generic.Dictionary<long, int>();
        CreateIcosphere();
        SurfaceTool st = new SurfaceTool();
        var am = CreateTheMesh();
        st.CreateFrom(am, 0);
        st.GenerateNormals();
        Mesh = st.Commit();
    }

    public override void _Ready()
    {
        Generate();
    }
}