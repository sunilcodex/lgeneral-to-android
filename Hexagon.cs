using UnityEngine;
using System.Collections;

namespace Engine
{
[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
public class Hexagon : MonoBehaviour
	{

		public int maxTextures;
		public int actualTexture;
		private float x0;
		private float x1;
		private float x2;
		private float x3;
		private float x4;
		private float x5;
	
		public void Start ()
		{
			MeshFilter meshFilter = GetComponent<MeshFilter> ();
			if (meshFilter == null) {
				Debug.LogError ("MeshFilter no añadida al objeto");
				return;
			}
			Vector3 p0 = new Vector3 (-0.5f, 0, Mathf.Sqrt (3) / 2);
			Vector3 p1 = new Vector3 (-1, 0, 0);
			Vector3 p2 = new Vector3 (-0.5f, 0, -Mathf.Sqrt (3) / 2);
			Vector3 p3 = new Vector3 (0.5f, 0, -Mathf.Sqrt (3) / 2);
			Vector3 p4 = new Vector3 (1, 0, 0);
			Vector3 p5 = new Vector3 (0.5f, 0, Mathf.Sqrt (3) / 2);
                
                
			Mesh mesh = meshFilter.sharedMesh;
			if (mesh == null) {
				meshFilter.mesh = new Mesh ();
				mesh = meshFilter.sharedMesh;
			}
			mesh.Clear ();
                
			mesh.vertices = new Vector3[]{p0,p1,p2,p3,p4,p5};
			mesh.triangles = new int[]{
                        0,2,1,
                        0,5,2,
                        2,5,3,
                        3,5,4

                };
			/*float initialX = (actualTexture-1)/maxTextures;
        		float factor = 1.0f/maxTextures;
		
                Vector2 uv0 = new Vector2((initialX+0.26f)*factor,1);
                Vector2 uv1 = new Vector2((initialX+0.1f)*factor,0.5f);
                Vector2 uv2 = new Vector2((initialX+0.27f)*factor,0);
                Vector2 uv3 = new Vector2((initialX+0.74f)*factor,0);
                Vector2 uv4 = new Vector2((initialX+0.95f)*factor,0.5f);
                Vector2 uv5 = new Vector2((initialX+0.74f)*factor,1);*/
                
		
			float factor = 1f / maxTextures;
			float dist = 1f * factor - 0f * factor;
			float epsilon = 0.03f;
			/*Vector2 uv0 = new Vector2((0.25f+epsilon)*factor+(actualTexture-1)*dist,1f);
                Vector2 uv1 = new Vector2((0f+epsilon)*factor+(actualTexture-1)*dist,0.5f);
                Vector2 uv2 = new Vector2((0.25f+epsilon)*factor+(actualTexture-1)*dist,0f);
                Vector2 uv3 = new Vector2((0.75f-epsilon)*factor+(actualTexture-1)*dist,0f);
                Vector2 uv4 = new Vector2((1f-epsilon)*factor+(actualTexture-1)*dist,0.5f);
                Vector2 uv5 = new Vector2((0.75f-epsilon)*factor+(actualTexture-1)*dist,1f);*/
		
			Vector2 uv0 = new Vector2 ((0.27f) * factor + (actualTexture - 1) * dist, 1f);
			Vector2 uv1 = new Vector2 ((0.15f) * factor + (actualTexture - 1) * dist, 0.5f);
			Vector2 uv2 = new Vector2 ((0.27f) * factor + (actualTexture - 1) * dist, 0f);
			Vector2 uv3 = new Vector2 ((0.73f) * factor + (actualTexture - 1) * dist, 0f);
			Vector2 uv4 = new Vector2 ((0.96f) * factor + (actualTexture - 1) * dist, 0.5f);
			Vector2 uv5 = new Vector2 ((0.735f) * factor + (actualTexture - 1) * dist, 1f);
		
			mesh.uv = new Vector2[]{
                                uv0,uv1,uv2,
                                uv3,uv4,uv5
                };
                
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			mesh.Optimize ();
		}

		// Use this for initialization
//        void Start () {
//
//        }

		// Update is called once per frame
		void Update ()
		{

		}
	}
}