/************************************************************************
* Copyright (c) 2018 Jason Holt Smith <bicarbon8@gmail.com>
*************************************************************************
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
*************************************************************************/
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

// [ExecuteAlways]
public class CelestialBody : MonoBehaviour
{
    public float radius;
    public float surfaceGravity;
    [Space]
    public int StartingSubdivision;
    public float[] SubdivisionDistances;
    [Space]
    public Material SphereMaterial;
    [Space]
    public bool UseNoiseForElevation;
    public FastNoise.NoiseType NoiseType;
    public bool SmoothNegativeElevations;
    public int NoiseSeed;
    public float StartingNoiseFrequency;
    public float StartingNoiseAmplitude;
    public Body body;

    private GameObject localCamera;
    private QuadFace[] faces;
    private QuadTriangleCache triangleCache;
    private bool updating = false;
    private Mesh mesh;

    private void Update()
    {
        if (surfaceGravity != 0)
        {
            body.mass = surfaceGravity * (radius * radius) / Constant.G;
        }

        StartCoroutine(UpdateFaces(localCamera.transform.position));
        Render();
    }

    //[Button(Mode = ButtonMode.DisabledInPlayMode, Spacing = ButtonSpacing.Before)]
    [ContextMenu("Generate")]
    private void Generate()
    {
        localCamera = GameObject.Find("LocalCamera");

        for (int i = this.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(this.transform.GetChild(i).gameObject);
        }

        // only allow odd numbers of subdivisions as this simplifies the maths
        if (StartingSubdivision % 2 == 0)
        {
            StartingSubdivision++;
        }

        faces = new QuadFace[6];
        triangleCache = new QuadTriangleCache(StartingSubdivision + 2);

        // create Front
        AddFace(QuadFaceType.ZPosFront, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);

        // create Left
        AddFace(QuadFaceType.XNegLeft, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);

        // create Right
        AddFace(QuadFaceType.XPosRight, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);

        // create Top
        AddFace(QuadFaceType.YPosTop, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);

        // create Bottom
        AddFace(QuadFaceType.YNegBottom, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);

        // create Back
        AddFace(QuadFaceType.ZNegBack, radius * 2, localCamera, StartingSubdivision, SubdivisionDistances, triangleCache);
        Render();
    }

#if (UNITY_EDITOR)
    // [Button(Mode = ButtonMode.DisabledInPlayMode, Spacing = ButtonSpacing.None)]
    [ContextMenu("Export Mesh")]
    private void ExportMesh()
    {
        if (EditorApplication.isPlaying) return;
        MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

            combine[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;

            i++;
        }

        mesh = new Mesh();

        mesh.CombineMeshes(combine);

        SaveMesh(mesh, gameObject.name, true, true);

        var scaledObject = GetComponent<Body>().scaledTransform;

        if (scaledObject.GetComponent<MeshFilter>() && scaledObject.GetComponent<MeshRenderer>())
        {
            scaledObject.GetComponent<MeshFilter>().mesh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Resources/Models/" + name + ".asset", typeof(Mesh));
            scaledObject.GetComponent<MeshRenderer>().material = GetComponent<CelestialBody>().SphereMaterial;
        }
        else
        {
            scaledObject.gameObject.AddComponent<MeshFilter>().mesh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Resources/Models/" + name + ".asset", typeof(Mesh));
            scaledObject.gameObject.AddComponent<MeshRenderer>().material = GetComponent<CelestialBody>().SphereMaterial;
        }
    }

    public void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        string path = "Assets/Resources/Models/" + name + ".asset";

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            MeshUtility.Optimize(meshToSave);

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }
#endif

    private void Start()
    {
        Generate();
    }

    private IEnumerator UpdateFaces(Vector3 playerPosition)
    {
        if (!updating)
        {
            updating = true;

            // perform subdivision if needed
            foreach (QuadFace face in faces)
            {
                if (face != null)
                {
                    yield return face.UpdateQuad(playerPosition);
                }
            }

            updating = false;
        }
        yield return null;
    }

    private void Render()
    {
        if (faces != null && faces.Any())
        {
            foreach (QuadFace face in faces)
            {
                if (face.ShouldRender())
                {
                    face.Render();
                }
            }
        }
    }

    private void AddFace(QuadFaceType type, float size, GameObject player, int startingSubdivisions, float[] subdivisionDistances, QuadTriangleCache cache)
    {
        string faceName = type.ToString();
        var empty = new GameObject(faceName);
        empty.transform.parent = gameObject.transform;
        empty.layer = gameObject.layer;
        empty.transform.position = transform.position;
        empty.transform.rotation = transform.rotation;

        switch (type)
        {
            case QuadFaceType.YPosTop:
                empty.transform.Rotate(Vector3.left, 90);
                empty.transform.Rotate(Vector3.up, 180);
                break;
            case QuadFaceType.YNegBottom:
                empty.transform.Rotate(Vector3.right, 90);
                empty.transform.Rotate(Vector3.up, 180);
                break;
            case QuadFaceType.XNegLeft:
                empty.transform.Rotate(Vector3.up, 90);
                break;
            case QuadFaceType.XPosRight:
                empty.transform.Rotate(Vector3.down, 90);
                break;
            case QuadFaceType.ZPosFront:
                empty.transform.Rotate(Vector3.up, 180);
                break;
        }
        empty.transform.Translate(Vector3.back * (size / 2));

        QuadFace face = empty.AddComponent<QuadFace>();
        face.Root = this;
        face.FaceType = type;
        face.Size = size;
        face.Player = player;
        face.Subdivisions = startingSubdivisions;
        face.SubdivisionDistances = subdivisionDistances;
        face.TriangleCache = cache;
        face.StartingNoiseFrequency = StartingNoiseFrequency;
        face.StartingNoiseAmplitude = StartingNoiseAmplitude;
        face.SmoothNegativeElevations = SmoothNegativeElevations;
        face.Active = true;
        face.Initialize();

        faces[(int)type] = face;
    }

    public QuadFace GetQuadFace(QuadFaceType type)
    {
        return faces[(int)type];
    }

    public Vector3 ApplyElevation(Vector3 v, Vector2 uv)
    {
        Vector3 curvedVert = v.normalized * radius;
        float elevation = 0f;
        if (UseNoiseForElevation)
        {
            Elevation.Instance.Noise.SetSeed(NoiseSeed);
            elevation = Elevation.Instance.Get(curvedVert, StartingNoiseAmplitude,
            StartingNoiseFrequency, SubdivisionDistances.Length, NoiseType);
        }
        else
        {
            if (SphereMaterial != null)
            {
                var parallaxMap = SphereMaterial.GetTexture("_ParallaxMap") as Texture2D;
                if (parallaxMap != null)
                {
                    elevation = Elevation.Instance.Get(uv, parallaxMap, StartingNoiseAmplitude);
                }
            }
        }
        if (SmoothNegativeElevations && elevation < 0)
        {
            elevation = Mathf.Abs(elevation / (SubdivisionDistances.Length / 2));
        }

        Vector3 elevatedVert = v.normalized * (radius + elevation);
        return elevatedVert;
    }
}

public enum QuadFaceType
{
    ZPosFront = 0,
    XNegLeft = 1,
    ZNegBack = 2,
    XPosRight = 3,
    YPosTop = 4,
    YNegBottom = 5
}
