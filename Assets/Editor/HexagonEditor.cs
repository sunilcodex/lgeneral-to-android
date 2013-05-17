using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Hexagon))]
public class HexagonEditor : Editor {
	
	[MenuItem ("GameObject/Create Other/Hexagon")]
	static void Create ()
	{
		GameObject gameObject = new GameObject ("Hexagon");
		Hexagon s = gameObject.AddComponent<Hexagon>();
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter> ();
		meshFilter.mesh = new Mesh ();
		s.Start();
	}
}
