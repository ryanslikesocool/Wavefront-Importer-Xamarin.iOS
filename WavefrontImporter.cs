using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using SceneKit;

public class WavefrontImporter {
    string[] vertexLines;
    string[] normalLines;
    string[] indexLines;

    int polygonCount = 0;

    public Tuple<ModelStructure, SCNGeometry> ImportOBJAsObject(string path) {
        string[] fileContents = File.ReadLines(path).ToArray();

        GetLines(fileContents);
        polygonCount = indexLines.Length;

        int[] vertexIndexPrefix = SplitIndices(indexLines).Item1;
        int[] vertexIndices = SplitIndices(indexLines).Item2;
        int[] normalIndices = SplitIndices(indexLines).Item3;

        SCNVector3[] vertexPoints = LinesToPoints(vertexLines);
        SCNVector3[] normalPoints = LinesToPoints(normalLines);

        SCNVector3[] vertices = OrderPoints(vertexPoints, vertexIndices);
        SCNVector3[] normals = OrderPoints(normalPoints, normalIndices);

        vertexIndices = ReorderIndices(vertexIndexPrefix, vertexIndices);

        NSData vertexIndexData = IndexData(vertexIndices);

        SCNGeometrySource vertexSource = SCNGeometrySource.FromVertices(vertices);
        SCNGeometrySource normalSource = SCNGeometrySource.FromNormals(normals);
        SCNGeometryElement vertexElement = SCNGeometryElement.FromData(vertexIndexData, SCNGeometryPrimitiveType.Polygon, polygonCount, sizeof(int));

        ModelStructure modelData = new ModelStructure(vertexIndexPrefix, vertexIndices, vertexPoints, normalPoints);
        SCNGeometry geometry = SCNGeometry.Create(new[] { vertexSource, normalSource }, new[] { vertexElement });

        return new Tuple<ModelStructure, SCNGeometry>(modelData, geometry);
    }

    void GetLines(string[] file) {
        List<string> vLines = new List<string>();
        List<string> nLines = new List<string>();
        List<string> iLines = new List<string>();

        for (int i = 0; i < file.Length; i++) {
            if (file[i].StartsWith("v ", StringComparison.Ordinal)) {
                string trim = file[i].TrimStart('v', ' ');
                vLines.Add(trim);
            } else if (file[i].StartsWith("vn ", StringComparison.Ordinal)) {
                string trim = file[i].TrimStart('v', 'n', ' ');
                nLines.Add(trim);
            } else if (file[i].StartsWith("f ", StringComparison.Ordinal)) {
                string trim = file[i].TrimStart('f', ' ');
                iLines.Add(trim);
            }
        }

        vertexLines = vLines.ToArray();
        normalLines = nLines.ToArray();
        indexLines = iLines.ToArray();
    }

    Tuple<int[], int[], int[]> SplitIndices(string[] lines) {
        int[] vertexIndicesPrefix = new int[lines.Length];
        int[] returnVertexIndices = new int[lines.Length];
        int[] returnNormalIndices = new int[lines.Length];

        for (int i = 0; i < lines.Length; i++) {
            string trim = lines[i].TrimStart('f', ' ');
            string[] indices = trim.Split(" ");

            vertexIndicesPrefix[i] = indices.Length;

            for (int j = 0; j < indices.Length; j++) {
                returnVertexIndices[i] = int.Parse(indices[j].Split("//")[0]) - 1;
                returnNormalIndices[i] = int.Parse(indices[j].Split("//")[1]) - 1;
            }
        }

        return new Tuple<int[], int[], int[]>(vertexIndicesPrefix, returnVertexIndices, returnNormalIndices);
    }

    SCNVector3[] LinesToPoints(string[] lines) {
        SCNVector3[] returnPoints = new SCNVector3[lines.Length];

        for (int i = 0; i < lines.Length; i++) {
            string[] parts = lines[i].Split(" ");
            SCNVector3 point = new SCNVector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            returnPoints[i] = point;
        }

        return returnPoints;
    }

    SCNVector3[] OrderPoints(SCNVector3[] points, int[] indices) {
        SCNVector3[] returnPoints = new SCNVector3[indices.Length];

        for (int i = 0; i < indices.Length; i++) {
            returnPoints[i] = points[indices[i]];
        }

        return returnPoints;
    }

    int[] ReorderIndices(int[] prefix, int[] indices) {
        int[] returnIndices = new int[prefix.Length + indices.Length];

        for (int i = 0; i < prefix.Length; i++) {
            returnIndices[i] = prefix[i];
        }

        for (int i = 0; i < indices.Length; i++) {
            returnIndices[i + prefix.Length] = i;
        }

        return returnIndices;
    }



    NSData IndexData(int[] indices) {
        byte[][] returnIndexArray = new byte[indices.Length][];
        for (int i = 0; i < indices.Length; i++) {
            returnIndexArray[i] = BitConverter.GetBytes(indices[i]);
        }
        NSData returnIndexData = NSData.FromArray(returnIndexArray.SelectMany(id => id).ToArray());

        return returnIndexData;
    }

    //Unnecessary, but here for reference
    /*public SCNVector3 CalculateNormal(SCNVector3 a, SCNVector3 b, SCNVector3 c) {
        return SCNVector3.Normalize(SCNVector3.Cross(b - a, c - a));
    }*/
}
