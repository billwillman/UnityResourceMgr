using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshUtil {

    public static Vector3[] GetWorldVertices(GameObject gameObject) {
        Vector3[] aVertices = null;
        SkinnedMeshRenderer skin;
        MeshFilter meshFilter;
        Mesh sharedMesh;

        if ((skin = gameObject.GetComponent<SkinnedMeshRenderer>()) != null) {
            if ((sharedMesh = skin.sharedMesh) == null) {
                return null;
            }

            aVertices = sharedMesh.vertices;

            BoneWeight[] aBoneWeights = sharedMesh.boneWeights;
            Matrix4x4[] aBindPoses = sharedMesh.bindposes;
            Transform[] aBones = skin.bones;

            if (aVertices == null || aBoneWeights == null || aBindPoses == null || aBones == null || aBoneWeights.Length == 0 || aBindPoses.Length == 0 || aBones.Length == 0) {
                return null;
            }

            for (int nVertex = 0; nVertex < aVertices.Length; nVertex++) {
                BoneWeight bw = aBoneWeights[nVertex];
                Vector4 v = aVertices[nVertex];
                v.w = 1;
                Vector3 v3World = aBones[bw.boneIndex0].localToWorldMatrix * aBindPoses[bw.boneIndex0] * v * bw.weight0
                + aBones[bw.boneIndex1].localToWorldMatrix * aBindPoses[bw.boneIndex1] * v * bw.weight1
                + aBones[bw.boneIndex2].localToWorldMatrix * aBindPoses[bw.boneIndex2] * v * bw.weight2
                + aBones[bw.boneIndex3].localToWorldMatrix * aBindPoses[bw.boneIndex3] * v * bw.weight3;

                aVertices[nVertex] = v3World;
            }
        } else if ((meshFilter = gameObject.GetComponent<MeshFilter>()) != null) {
            if ((sharedMesh = meshFilter.sharedMesh) == null) {
                return null;
            }

            aVertices = sharedMesh.vertices;

            if (aVertices == null) {
                return null;
            }

            for (int nVertex = 0; nVertex < aVertices.Length; nVertex++) {
                aVertices[nVertex] = gameObject.transform.TransformPoint(aVertices[nVertex]);
            }
        }

        return aVertices;
    }


    public static bool HasValidMeshData(GameObject go) {
        return go.GetComponent<MeshFilter>() != null || go.GetComponent<SkinnedMeshRenderer>() != null;
    }
}
