using UnityEngine;
using System.Collections;
using Miscellaneous;

[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
public class Hexagon : MonoBehaviour
{

        
        public void Start ()
        {
                MeshFilter meshFilter = GetComponent<MeshFilter> ();
                if (meshFilter == null) {
                        throw new MissingComponentException ("MeshFilter no añadida al objeto");
                }
		
				Vector3 p0 = new Vector3 (-Config.hex_w/4, 0, Config.hex_h / 2);
                Vector3 p1 = new Vector3 (-Config.hex_w/2, 0, 0);
                Vector3 p2 = new Vector3 (-Config.hex_w/4, 0, -Config.hex_h  / 2);
                Vector3 p3 = new Vector3 (Config.hex_w/4, 0, -Config.hex_h / 2);
                Vector3 p4 = new Vector3 (Config.hex_w/2, 0, 0);
                Vector3 p5 = new Vector3 (Config.hex_w/4, 0, Config.hex_h / 2);
                
                
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
                
		
				Vector2 uv0 = new Vector2 (0.255f, 1);
                Vector2 uv1 = new Vector2 (0.15f, 0.5f);
                Vector2 uv2 = new Vector2 (0.255f, 0);
                Vector2 uv3 = new Vector2 (0.74f, 0);
                Vector2 uv4 = new Vector2 (0.96f, 0.5f);
                Vector2 uv5 = new Vector2 (0.74f, 1);
                mesh.uv = new Vector2[]{
                                uv0,uv1,uv2,
                                uv3,uv4,uv5
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