using UnityEngine;
using System.Collections.Generic;

[RequireComponent( typeof(MeshFilter) )]
[RequireComponent( typeof(MeshRenderer) )]
public class Decal : MonoBehaviour {

	public Material material;
	public Sprite sprite;

	public float maxAngle = 90.0f;
	public float pushDistance = 0.009f;
	public LayerMask affectedLayers = -1;
	private GameObject[] affectedObjects;

	private Matrix4x4 oldMatrix;
	private Vector3 oldScale;

	void OnDrawGizmosSelected() {
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube( Vector3.zero, Vector3.one );
	}

	public Bounds GetBounds() {
		Vector3 size = transform.lossyScale;
		Vector3 min = -size/2f;
		Vector3 max =  size/2f;

		Vector3[] vts = new Vector3[] {
			new Vector3(min.x, min.y, min.z),
			new Vector3(max.x, min.y, min.z),
			new Vector3(min.x, max.y, min.z),
			new Vector3(max.x, max.y, min.z),

			new Vector3(min.x, min.y, max.z),
			new Vector3(max.x, min.y, max.z),
			new Vector3(min.x, max.y, max.z),
			new Vector3(max.x, max.y, max.z),
		};

		for(int i=0; i<8; i++) {
			vts[i] = transform.TransformDirection( vts[i] );
		}

		min = max = vts[0];
		foreach(Vector3 v in vts) {
			min = Vector3.Min(min, v);
			max = Vector3.Max(max, v);
		}

		return new Bounds(transform.position, max-min);
	}

	// Update is called once per frame
	void Update() {
		// Only rebuild mesh when scaling
		//bool hasChanged = oldMatrix != transform.localToWorldMatrix;
		bool hasChanged = oldScale != transform.localScale;
		//oldMatrix = transform.localToWorldMatrix;
		oldScale = transform.localScale;
		
		
		if(hasChanged) {
			BuildDecal( this );
		}

	}

	public void BuildDecal(Decal decal) {
		MeshFilter filter = decal.GetComponent<MeshFilter>();
		if(filter == null) filter = decal.gameObject.AddComponent<MeshFilter>();
		if(decal.renderer == null) decal.gameObject.AddComponent<MeshRenderer>();
		//decal.renderer.material = decal.material;
		decal.material = decal.renderer.material;
		
		if(decal.material == null || decal.sprite == null) {
			filter.mesh = null;
			return;
		}
		
		affectedObjects = GetAffectedObjects(decal.GetBounds(), decal.affectedLayers);
		foreach(GameObject go in affectedObjects) {
			DecalBuilder.BuildDecalForObject( decal, go );
		}
		DecalBuilder.Push( decal.pushDistance );
		
		Mesh mesh = DecalBuilder.CreateMesh();
		if(mesh != null) {
			mesh.name = "DecalMesh";
			filter.mesh = mesh;
		}
	}
		
	private static bool IsLayerContains(LayerMask mask, int layer) {
		//Debug.Log("Mask value is " + mask.value);
		//Debug.Log("Layer value is " + (layer >> 2));
		if (mask.value >= 0)
			return ((mask.value >> 2) & layer) != 0;
		else
			return true;
	}

	private static GameObject[] GetAffectedObjects(Bounds bounds, LayerMask affectedLayers) {
		MeshRenderer[] renderers = (MeshRenderer[]) GameObject.FindObjectsOfType<MeshRenderer>();
		List<GameObject> objects = new List<GameObject>();
		foreach(Renderer r in renderers) {
			if( !r.enabled ) continue;
            /*
            if (r.gameObject.name == "bonnet") {
                Debug.Log("bonnet layer is " + r.gameObject.layer);
                //int test = (affectedLayers.value >> 2);
                Debug.Log("affected layer is " + (affectedLayers.value >> 2));

                Debug.Log("Mask test: " + (affectedLayers.value & r.gameObject.layer >> 2));
                //Debug.Log("Mask test: " + (r.gameObject.layer & (affectedLayers.value >> 2)));
            }
            */
			if( !IsLayerContains(affectedLayers, r.gameObject.layer) ) continue;
			if( r.GetComponent<Decal>() != null ) continue;
			
			if( bounds.Intersects(r.bounds) ) {
				objects.Add(r.gameObject);
			}
		}
		return objects.ToArray();
	}
}