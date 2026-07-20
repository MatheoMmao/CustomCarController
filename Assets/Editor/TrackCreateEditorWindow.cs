using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class TrackCreateEditorWindow : EditorWindow
{
    GameObject Track;
    GameObject checkTrackChange;
    List<GameObject> trackParts = new List<GameObject>();
    GameObject trackPartPrefab;

    int accuracy;

    TrackEditEditorWindow editWindow;

    [MenuItem("Tools/Create Track")]
    public static void OpenTrackCreateEditorWindow()
    {
        GetWindow<TrackCreateEditorWindow>("Create Track").Init();
    }

    void Init()
    {
        trackPartPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/TrackPart.prefab", typeof(GameObject)) as GameObject;
    }

    private void OnGUI()
    {
        Track = EditorGUILayout.ObjectField(Track, typeof(GameObject), true) as GameObject;
        if (Track == null)
        {
            GUI.color = Color.red;
            GUILayout.Label("You must put a track base to create a track");
            GUI.color = Color.white;
        }
        else if (trackPartPrefab == null)
        {
            GUI.color = Color.red;
            GUILayout.Label("Track part prefab can't be found");
            GUI.color = Color.white;
            trackPartPrefab = EditorGUILayout.ObjectField(trackPartPrefab, typeof(GameObject), false) as GameObject;
        }
        else
        {
            CheckTrackChange();

            GUILayout.BeginHorizontal(GUILayout.Width(300));
            GUILayout.Label("Track Accuracy");
            accuracy = (int)EditorGUILayout.Slider(accuracy, 1, 100);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(200));
            if (GUILayout.Button("Edit Track Layout"))
            {
                editWindow = TrackEditEditorWindow.OpenTrackEditEditorWindow();
                editWindow.SetNewTrack(Track);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(200));
            if (GUILayout.Button("Create NewTrack"))
            {
                CreateTrackParts();
            }
            GUILayout.EndHorizontal();
        }
    }

    void CheckTrackChange()
    {
        if (checkTrackChange != Track)
        {
            checkTrackChange = Track;

            if (editWindow != null)
            {
                if (!Track.GetComponent<MeshFilter>()) Track.AddComponent<MeshFilter>();
                if (!Track.GetComponent<MeshRenderer>()) Track.AddComponent<MeshRenderer>();
                editWindow.SetNewTrack(Track);
            }
        }
    }

    void CreateTrackParts()
    {
        trackParts.Clear();

        List<Transform> bezierDots = new List<Transform>();

        foreach (Transform child in Track.transform)
        {
            if (child.tag.Contains("TrackPart"))
            {
                trackParts.Add(child.gameObject);
            }
            if (child.tag.Contains("dotsTrack"))
            {
                bezierDots.Add(child);
            }
        }

        if (bezierDots.Count < 2)
        {
            return;
        }

        List<Vector3> checkPointsTrack = new List<Vector3>();

        for (int i = 1; i < bezierDots.Count; i++)
        {
            Vector3[] arrayBezier = Handles.MakeBezierPoints(bezierDots[i - 1].localPosition, bezierDots[i].localPosition,
                -bezierDots[i - 1].localScale * 2 + bezierDots[i - 1].localPosition, bezierDots[i].localScale * 2 + bezierDots[i].localPosition, accuracy);

            for (int y = 0; y < arrayBezier.Length; y++)
            {
                arrayBezier[y] = new Vector3(arrayBezier[y].x, arrayBezier[y].z, -arrayBezier[y].y);
                checkPointsTrack.Add(arrayBezier[y]);
            }
        }

        MeshBuilder mb = new MeshBuilder();

        Vector3 previousForward = Vector3.zero;
        Vector3 previousRight = Vector3.zero;
        Vector3 previousPos = Vector3.zero;

        float width = 10;

        for (int i = 0; i < checkPointsTrack.Count; i++)
        {
            if (i == 0 && i < checkPointsTrack.Count - 1)
            {
                previousForward = checkPointsTrack[i] - checkPointsTrack[i + 1];
                previousRight = Vector3.Cross(Vector3.up, previousForward).normalized;
                previousPos = checkPointsTrack[i] - previousRight / 2;
                continue;
            }
            Vector3 forward = i < checkPointsTrack.Count-1 ? checkPointsTrack[i] - checkPointsTrack[i + 1] : previousForward;

            Vector3 mixForward = previousForward + forward;

            Vector3 right = Vector3.Cross(Vector3.up, mixForward).normalized;

            Vector3 pos = checkPointsTrack[i] - right/2;

            mb.BuildQuad(previousPos, previousPos + previousRight*width, pos + right*width, pos, Vector3.up);

            previousRight = right;
            previousForward = forward;
            previousPos = pos;
        }

        MeshFilter filter = Track.GetComponent<MeshFilter>();
        MeshCollider collider = Track.GetComponent<MeshCollider>();

        Mesh mesh = mb.CreateMesh();

        if (filter)
        {
            filter.sharedMesh = mesh;
        }
        if (collider)
        {
            collider.sharedMesh = mesh;
        }

        AssetDatabase.CreateAsset(mesh, $"Assets/GeneratedTracks/{Track.name}.asset");
        AssetDatabase.SaveAssets();



        //while (trackParts.Count > idTrackPart)
        //{
        //    DestroyImmediate(trackParts[idTrackPart]);
        //    trackParts.RemoveAt(idTrackPart);
        //}
    }
}
