using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;
public class TestUV : MonoBehaviour
{

    public Spline spline;
    public SplineMeshTiling tiling;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < tiling.mesh.uv.Length; i++)
        {
            var sample = spline.GetProjectionSample(tiling.mesh.vertices[i]);
            tiling.mesh.uv[i].y = sample.distanceInCurve;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
