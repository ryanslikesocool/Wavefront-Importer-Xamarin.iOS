using System;
using System.Collections.Generic;
using SceneKit;
using System.Linq;
using Foundation;
using System.IO;

public static class WavefrontImporter {
    public static SCNGeometry ImportOBJ(string path) {
        string resourcesPath = NSBundle.MainBundle.ResourcePath;
        string[] fileContents = File.ReadLines(resourcesPath + path).ToArray();

        int[] vertexIndices = OBJVertexIndices(fileContents);
        int[] normalIndices = OBJNormalIndices(fileContents);
        SCNVector3[] vertices = OBJExtendedVertices(OBJVertices(fileContents), vertexIndices);
        SCNVector3[] normals = OBJExtendedNormals(OBJNormals(fileContents), normalIndices);

        NSData vertexIndexData = IndexData(vertexIndices);
        NSData normalIndexData = IndexData(normalIndices);

        SCNGeometrySource vertexSource = SCNGeometrySource.FromVertices(vertices);
        SCNGeometrySource normalSource = SCNGeometrySource.FromNormals(normals);
        SCNGeometryElement vertexElement = SCNGeometryElement.FromData(vertexIndexData, SCNGeometryPrimitiveType.Triangles, vertexIndices.Length, sizeof(int));
        SCNGeometryElement normalElement = SCNGeometryElement.FromData(normalIndexData, SCNGeometryPrimitiveType.Triangles, normalIndices.Length, sizeof(int));

        return SCNGeometry.Create(new[] { vertexSource, normalSource }, new[] { vertexElement, normalElement });
    }

    public static SCNVector3[] OBJVertices(string[] file) {
        List<SCNVector3> returnPoints = new List<SCNVector3>();

        foreach (string line in file) {
            if (line.StartsWith("v ", StringComparison.Ordinal)) {
                string trim = line.TrimStart('v', ' ');
                string[] points = trim.Split(" ");

                returnPoints.Add(new SCNVector3(float.Parse(points[0]), float.Parse(points[1]), float.Parse(points[2])));
            }
        }
     
        return returnPoints.ToArray();
    }

    public static SCNVector3[] OBJExtendedVertices(SCNVector3[] vertices, int[] indices) {
        List<SCNVector3> returnPoints = new List<SCNVector3>();

        for (int i = 0; i < indices.Length; i++) {
            returnPoints.Add(vertices[indices[i]]);
        }

        return returnPoints.ToArray();
    }

    public static SCNVector3[] OBJNormals(string[] file) {
        List<SCNVector3> returnNormals = new List<SCNVector3>();

        foreach (string line in file) {
            if (line.StartsWith("vn ", StringComparison.Ordinal)) {
                string trim = line.TrimStart('v', 'n', ' ');
                string[] points = trim.Split(" ");

                returnNormals.Add(SCNVector3.Normalize(new SCNVector3(float.Parse(points[0]), float.Parse(points[1]), float.Parse(points[2]))));
            }
        }

        return returnNormals.ToArray();
    }

    public static SCNVector3[] OBJExtendedNormals(SCNVector3[] normals, int[] indices) {
        List<SCNVector3> returnPoints = new List<SCNVector3>();

        for (int i = 0; i < indices.Length; i++) {
            returnPoints.Add(normals[indices[i]]);
        }

        return returnPoints.ToArray();
    }

    public static int[] OBJVertexIndices(string[] file) {
        List<int> returnIndices = new List<int>();

        foreach (string line in file) {
            if (line.StartsWith("f", StringComparison.Ordinal)) {
                string trim = line.TrimStart('f', ' ');
                string[] indices = trim.Split(" ");

                for (int i=0; i < indices.Length; i++) {
                    indices[i] = indices[i].Split("//")[0];
                }

                for (int i = 0; i < 3; i++) {
                    returnIndices.Add(int.Parse(indices[i]) - 1);
                }

                if (indices.Length == 4) {
                    returnIndices.Add(int.Parse(indices[0]) - 1);

                    for (int i = 2; i < 4; i++) {
                        returnIndices.Add(int.Parse(indices[i]) - 1);
                    }
                }
            }
        }

        return returnIndices.ToArray();
    }

    public static int[] OBJNormalIndices(string[] file) {
        List<int> returnIndices = new List<int>();

        foreach (string line in file) {
            if (line.StartsWith("f", StringComparison.Ordinal) && line.Contains("//")) {
                string trim = line.TrimStart('f', ' ');
                string[] indices = trim.Split(" ");

                for (int i = 0; i < indices.Length; i++) {
                    indices[i] = indices[i].Split("//")[1];
                }

                for (int i = 0; i < 3; i++) {
                    returnIndices.Add(int.Parse(indices[i]) - 1);
                }

                if (indices.Length == 4) {
                    returnIndices.Add(int.Parse(indices[0]) - 1);

                    for (int i = 2; i < 4; i++) {
                        returnIndices.Add(int.Parse(indices[i]) - 1);
                    }
                }
            }
        }

        return returnIndices.ToArray();
    }

    public static NSData IndexData(int[] indices) {
        byte[][] returnIndexArray = new byte[indices.Length][];
        for (int i = 0; i < indices.Length; i++) {
            returnIndexArray[i] = BitConverter.GetBytes(indices[i]);
        }
        NSData returnIndexData = NSData.FromArray(returnIndexArray.SelectMany(id => id).ToArray());

        return returnIndexData;
    }
}
