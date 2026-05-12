using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MeshGenerator : MonoBehaviour {

    [SerializeField] private float _wallHeight = 5f;
    [SerializeField] private MeshFilter _walls;

    private SquareGrid _squareGrid;

    private List<Vector3> _vertices;
    private List<int> _triangles;

    Dictionary<int, List<Triangle>> _triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> _outlines = new List<List<int>>();
    HashSet<int> _checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] map, float squareSize) {

        _triangleDictionary.Clear();
        _outlines.Clear();
        _checkedVertices.Clear();

        _squareGrid = new SquareGrid(map, squareSize);

        _vertices = new List<Vector3>();
        _triangles = new List<int>();

        for (int x = 0; x < _squareGrid.squares.GetLength(0); x++) {
            for (int y = 0; y < _squareGrid.squares.GetLength(1); y++) {
                TriangulateSquare(_squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.RecalculateNormals();

        CreateWallMesh();
    }

    private void CreateWallMesh() {

        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        foreach(List<int> outline in _outlines){
            for(int i=0; i<outline.Count - 1; i++) {
                int startIndex = wallVertices.Count;
                wallVertices.Add(_vertices[outline[i]]);      // left
                wallVertices.Add(_vertices[outline[i+1]]);    // right
                wallVertices.Add(_vertices[outline[i]] - Vector3.up * _wallHeight);      // bottom left
                wallVertices.Add(_vertices[outline[i+1]] - Vector3.up * _wallHeight);    // bottom right

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();

        _walls.mesh = wallMesh;
    }

    private void TriangulateSquare(Square square) {
        switch (square.configuration) {
            case 0:
                break;

            // 1 point
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            // 2 points
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 3 points
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 4 points
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                _checkedVertices.Add(square.topLeft.vertexIndex);
                _checkedVertices.Add(square.topRight.vertexIndex);
                _checkedVertices.Add(square.bottomRight.vertexIndex);
                _checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    private void MeshFromPoints(params Node[] points) {
        AssignVertices(points);

        if(points.Length >= 3) CreateTriangle(points[0], points[1], points[2]);
        if(points.Length >= 4) CreateTriangle(points[0], points[2], points[3]);
        if(points.Length >= 5) CreateTriangle(points[0], points[3], points[4]);
        if(points.Length >= 6) CreateTriangle(points[0], points[4], points[5]);

    }

    private void AssignVertices(Node[] points) {
        for(int i=0; i<points.Length; i++) {
            if(points[i].vertexIndex == -1) {
                points[i].vertexIndex = _vertices.Count;
                _vertices.Add(points[i].position);
            }
        }
    }

    private void CreateTriangle(Node a, Node b, Node c) {
        _triangles.Add(a.vertexIndex);
        _triangles.Add(b.vertexIndex);
        _triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
        if(_triangleDictionary.ContainsKey(vertexIndexKey)) {
            _triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            _triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    private void CalculateMeshOutlines() {

        for(int vertexIndex=0; vertexIndex<_vertices.Count; vertexIndex++) {
            if (!_checkedVertices.Contains(vertexIndex)) {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1) {
                    _checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    _outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, _outlines.Count - 1);
                    _outlines[_outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    private void FollowOutline(int vertexIndex, int outlineIndex) {
        _outlines[outlineIndex].Add(vertexIndex);
        _checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex) {
        List<Triangle> trianglesContainingVertex = _triangleDictionary[vertexIndex];

        for(int i=0; i<trianglesContainingVertex.Count; i++) {
            Triangle triangle = trianglesContainingVertex[i];

            for(int j=0; j<3; j++) {
                int vertexB = triangle[j];

                if (vertexB != vertexIndex && !_checkedVertices.Contains(vertexB)) {
                    if (IsOutlineEdge(vertexIndex, vertexB)) {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    private bool IsOutlineEdge(int vertexA, int vertexB) {
        List<Triangle> trianglesContainingVertexA = _triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i=0; i<trianglesContainingVertexA.Count; i++) {
            if (trianglesContainingVertexA[i].Contains(vertexB)) {
                sharedTriangleCount++;
                if(sharedTriangleCount > 1) {
                    break;
                }
            }
        }

        return sharedTriangleCount == 1;
    }


    struct Triangle {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public Triangle(int a, int b, int c) {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = vertexIndexA;
            vertices[1] = vertexIndexB;
            vertices[2] = vertexIndexC;
        }

        public int this[int i] {
            get {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex) {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    public class SquareGrid {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize) {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for(int x=0; x<nodeCountX; x++) {
                for(int y=0; y<nodeCountY; y++) {
                    Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX-1; x++) {
                for (int y = 0; y < nodeCountY-1; y++) {
                    squares[x, y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1, y], controlNodes[x,y]);
                }
            }
        }
    }

    public class Square {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration = 0;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            configuration += topLeft.active ? 8 : 0;
            configuration += topRight.active ? 4 : 0;
            configuration += bottomRight.active ? 2 : 0;
            configuration += bottomLeft.active ? 1 : 0;
        }
    }

    public class Node {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos) {
            position = _pos;
        }
    }

    public class ControlNode : Node {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
            active = _active;
            above = new Node (position + Vector3.forward * squareSize / 2f);
            right = new Node (position + Vector3.right * squareSize / 2f);
        }
    }
}
