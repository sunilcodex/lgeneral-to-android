using UnityEngine;
using System.Collections;
using Miscellaneous;
using EngineApp;

[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
public class HorizontalPlane : MonoBehaviour
{

	// Use this for initialization
	public void Start ()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter> ();
		if (meshFilter == null) {
			throw new MissingComponentException ("MeshFilter no añadida al objeto");
		}
		Vector3 p0 = new Vector3 (0, 1, 0);
		Vector3 p1 = new Vector3 (0, 1, ((float)Config.hex_h / 2));
		Vector3 p2 = new Vector3 (((float)(Config.hex_x_offset * Engine.map.map_w) - Config.hex_x_offset), 1, ((float)Config.hex_h / 2));
		Vector3 p3 = new Vector3 (((float)(Config.hex_x_offset * Engine.map.map_w) - Config.hex_x_offset), 1, 0);
		Mesh mesh = meshFilter.sharedMesh;
		if (mesh == null) {
			meshFilter.mesh = new Mesh ();
			mesh = meshFilter.sharedMesh;
		}
		mesh.Clear ();
		mesh.vertices = new Vector3[]{p0,p1,p2,p3};
		mesh.triangles = new int[]{
                        0,1,2,
						0,2,3

        };
		Vector2 uv0 = new Vector2 (0, 1);
        Vector2 uv1 = new Vector2 (0, 0);
        Vector2 uv2 = new Vector2 (1, 0);
        Vector2 uv3 = new Vector2 (1, 1);
		mesh.uv = new Vector2[]{
                                uv0,uv1,
                                uv2,uv3
                };
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		mesh.Optimize ();
		MeshCollider meshCollider = GetComponent<MeshCollider> ();
		meshCollider.sharedMesh = mesh;
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}
}
