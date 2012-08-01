using UnityEngine;
using System.Collections;

public class HexagonMesh : MonoBehaviour {
	
	void Awake(){
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter==null){
			Debug.LogError("MeshFilter no añadida al objeto");
			return;
		}
		Vector3 p0 = new Vector3(-Mathf.Sqrt(3)/2,0,0.5f);
		Vector3 p1 = new Vector3(-Mathf.Sqrt(3)/2,0,-0.5f);
		Vector3 p2 = new Vector3(0,0,-1);
		Vector3 p3 = new Vector3(Mathf.Sqrt(3)/2,0,-0.5f);
		Vector3 p4 = new Vector3(Mathf.Sqrt(3)/2,0,0.5f);
		Vector3 p5 = new Vector3(0,0, 1);
		
		
		Mesh mesh = meshFilter.sharedMesh;
		if (mesh == null){
			meshFilter.mesh = new Mesh();
			mesh = meshFilter.sharedMesh;
		}
		mesh.Clear();
		
		mesh.vertices = new Vector3[]{p0,p1,p2,p3,p4,p5};
		mesh.triangles = new int[]{
			0,5,4,
			0,4,1,
			1,4,3,
			1,3,2

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
