using UnityEngine;
using System.Collections;


[RequireComponent (typeof (MeshCollider))]
[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshRenderer))]
public class Hexagon : MonoBehaviour {

	
	public void Awake(){
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter==null){
			Debug.LogError("MeshFilter no añadida al objeto");
			return;
		}
		Vector3 p0 = new Vector3(-0.5f,0,Mathf.Sqrt(3)/2);
		Vector3 p1 = new Vector3(-1,0,0);
		Vector3 p2 = new Vector3(-0.5f,0,-Mathf.Sqrt(3)/2);
		Vector3 p3 = new Vector3(0.5f,0,-Mathf.Sqrt(3)/2);
		Vector3 p4 = new Vector3(1,0,0);
		Vector3 p5 = new Vector3(0.5f,0,Mathf.Sqrt(3)/2);
		
		
		Mesh mesh = meshFilter.sharedMesh;
		if (mesh == null){
			meshFilter.mesh = new Mesh();
			mesh = meshFilter.sharedMesh;
		}
		mesh.Clear();
		
		mesh.vertices = new Vector3[]{p0,p1,p2,p3,p4,p5};
		mesh.triangles = new int[]{
			0,2,1,
			0,5,2,
			2,5,3,
			3,5,4

		};
	
		Vector2 uv0 = new Vector2(0.25f,1);
		Vector2 uv1 = new Vector2(0,0.5f);
		Vector2 uv2 = new Vector2(0.25f,0);
		Vector2 uv3 = new Vector2(0.75f,0);
		Vector2 uv4 = new Vector2(1,0.5f);
		Vector2 uv5 = new Vector2(0.75f,1);
		
		mesh.uv = new Vector2[]{
				uv0,uv1,uv2,
				uv3,uv4,uv5
		};
		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}
}