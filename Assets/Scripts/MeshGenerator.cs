using UnityEngine;

//generates meshData from appropriate data passed in
public static class MeshGenerator {
    //generates meshData from appropriate data passed in
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) {
        //how many vertices to skip, scales with LOD (this being 2 means we skip every other vertex)
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        //number of verts to a side in the mesh
        int numVertsPerLine = meshSettings.numVertsPerLine;

        //Top left X and Z of the verticies in the soon-to-be-created meshData
        Vector2 topLeft = new Vector2(-1, -1) * meshSettings.meshWorldSize / 2f;

        //creates a new meshdata with the correct amount of verticies for the level of detail specified
        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        /*the map of verticies for dealing with the lighting errors that occur at the edge of each mesh looks like this on a 3x3 mesh
         * -1 -2  -3  -4  -5
         * -6  0   1   2  -7
         * -8  3   4   5  -9
         *-10  6   7   8  -11
         *-12 -13 -14 -15 -16
         *because negative indices vertexes aren't included in the final mesh. This is obviously hard to create so that's what these variables and for loops are for
         */
        int[,] vertexIndiciesMap = new int[numVertsPerLine, numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        //creating the map of vertices
        for (int y = 0; y < numVertsPerLine; y ++) {
            for (int x = 0; x < numVertsPerLine; x ++) {
                //check if we're on the outside border fo the mesh
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                //check if we're skipping the verice for LOD stuff
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (isOutOfMeshVertex) {
                    vertexIndiciesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                } else if (!isSkippedVertex) {
                    vertexIndiciesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        //runs through each vertice according to LOD
        for (int y = 0; y < numVertsPerLine; y ++) {
            for (int x = 0; x < numVertsPerLine; x ++) {
                //check if we're skipping the verice for LOD stuff
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex) {
                    //check what kind of vertex we are
                    //https://www.desmos.com/calculator/bcagu8b0vn
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    //gets the correct vertexIndex from the indicies map we just made
                    int vertexIndex = vertexIndiciesMap[x, y];
                    //used to map the texture to the mesh
                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    //we flip y in order to alighn the unity editor z axis with the programming y axis
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
                    //the height of the vertex at the given x, y coordinates
                    float height = heightMap[x, y];

                    //this is to make the edgeconnection vertices match up with the mesh when it's at a different LOD by linearly interpolating between the 2 closest vertices' heights
                    if (isEdgeConnectionVertex) {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement; // distance to the closest main vertex above
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA; // distance to the closest main vertex below

                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y];
                        float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y];

                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    //should we create a triangle
                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    //if we aren't on the finishing edges/half of the outside edge
                    if (createTriangle) {
                        //the size of the triangle
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        //the indicies our 2 triangles are being made of
                        int a = vertexIndiciesMap[x, y];
                        int b = vertexIndiciesMap[x + currentIncrement, y];
                        int c = vertexIndiciesMap[x, y + currentIncrement];
                        int d = vertexIndiciesMap[x + currentIncrement, y + currentIncrement];
                        //adding triangles to the mesh Data
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        //calculate the normals
        meshData.ProcessMesh();

        //return the meshData
        return meshData;
    }
}

//holds all the data for a mesh
public class MeshData {
    //the verticies of the mesh and an array that holds the indexes of theose verticies in the verticies array in sets of 3 to make triangles
    Vector3[] vertices;
    int[] triangles;
    //used to map a texture to the mesh
    Vector2[] uvs;

    //used to calculate surface normals, won't be included in the final mesh
    Vector3[] outOfMeshVerticies;
    int[] outOfMeshTriangles;

    //for keeping track of how many triangles we've added to both triangles arrays
    int triangleIndex;
    int outOfMeshTriangleIndex;

    //baked means built-in
    Vector3[] bakedNormals;

    //do we use the flatshading for graphics
    bool useFlatShading;

    //construct a meshData from the # of vetices in a line, the skipIncrement, and whether to use flatshading
    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        //calculating each of these variables
        int numMeshEdgeVerticies = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVerticies = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticiesPerLine = (numVertsPerLine -5) / skipIncrement + 1;
        int numMainVerticies = numMainVerticiesPerLine * numMainVerticiesPerLine;

        //setting the number of vertices and uvs
        vertices = new Vector3[numMeshEdgeVerticies + numEdgeConnectionVerticies + numMainVerticies];
        uvs = new Vector2[vertices.Length]; ;

        //calcualting the number of triangles
        int numMeshEdgeTriangles = (numVertsPerLine - 4) * 8;
        int numMainTriangles = (numMainVerticiesPerLine - 1) * (numMainVerticiesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVerticies = new Vector3[numVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            //is a border vertex
            outOfMeshVerticies[-vertexIndex - 1] = vertexPosition;
        } else {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    //adds a triangle using vertex indicies
    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) {
            outOfMeshTriangles[outOfMeshTriangleIndex] = a;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
        } else {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    //calculates the normals of the meshData
    Vector3[] CalculateNormals() {

        Vector3[] vertexNormals = new Vector3[vertices.Length];

        //calculates the normals of the main triangles
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++) {
            //get the indicies of the vertexes for each triangle
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            //calculate the triangle's normal
            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC);

            //this is how we average the normals of the triangles around each vertex
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        //calculates the normals of the outside-of-mesh triangles that are used to fix the lighting error on the outside edge of the mesh
        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            //get the indicies of the vertexes for each triangle
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

            //calculate the triangle's normal
            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC);

            //make sure that we only deal with vertexes that actually exist in the array
            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        //normalize all the vectors
        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    //returns the surface normal of a triangle from its 3 vertices (via their indexes in the triangles array)
    Vector3 SurfaceNormalFromIndicies(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? outOfMeshVerticies[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? outOfMeshVerticies[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? outOfMeshVerticies[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointA - pointB;
        Vector3 sideAC = pointA - pointC;

        //the cross product of 2 vectors is a vector perpendicular to them both
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    //deals with the proper shading method
    public void ProcessMesh() {
        if (useFlatShading) {
            Flatshading();
        } else {
            //if we're flatshading the verticies, then we don't need to share verticies across meshes, (in fact it takes less processing time if we don't) and the problem BakeNormals() was supposed to fix doesn't come up anyways
            BakeNormals();
        }
    }

    void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    //for when we want to have the endless terrain be flatshaded (look it up if you don't remember)
    void Flatshading() {
        /*
         * the array that holds the verticies for the triangles
         * it's usually stored like so for our meshes
         * 0---2
         * | / |
         * 1---3
         * would be stored as 0, 1, 2 and 1, 2, 3, but for flatshading we want them like
         * 0---(2/4)
         * |  /    |
         * (1/3)---5
         * so stored as 0 1 2, and 3 4 5
         * this Vec3 array stores the verticies, and we just re-use the triangles array
         * we also have to fix the uvs, so they get their own array
         */
        Vector3[] flatShadedVerticies = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        //this block of code does the rearranging
        for (var i = 0; i < triangles.Length; i++) {
            flatShadedVerticies[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVerticies;
        uvs = flatShadedUvs;
    }

    //creates a mesh from the meshData
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}