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
                -bezierDots[i - 1].localScale*2 + bezierDots[i - 1].localPosition, bezierDots[i].localScale*2 + bezierDots[i].localPosition, accuracy);

            for (int y = 0; y < arrayBezier.Length; y++)
            {
                arrayBezier[y] = new Vector3(arrayBezier[y].x, arrayBezier[y].z, -arrayBezier[y].y);
                checkPointsTrack.Add(arrayBezier[y]);
            }
        }

        int idTrackPart = 0;
        for (int i = 1; i < checkPointsTrack.Count; i++)
        {
            Vector3 gapPoints = (checkPointsTrack[i] - checkPointsTrack[i - 1]);
            Vector3 centerPosition = checkPointsTrack[i - 1] + gapPoints * 0.5f;

            Vector3 gapPointNorm = gapPoints.normalized;
            float angle = Mathf.Atan2(gapPointNorm.x, gapPointNorm.z) * Mathf.Rad2Deg;

            if (idTrackPart < trackParts.Count)
            {
                trackParts[idTrackPart].transform.localPosition = centerPosition;
                trackParts[idTrackPart].transform.localRotation = Quaternion.Euler(0,angle, 0);
                trackParts[idTrackPart].transform.localScale = new Vector3(1, 1, gapPoints.magnitude / 10+1);
            }
            else
            {
                trackParts.Add(Instantiate(trackPartPrefab, centerPosition, Quaternion.Euler(0, angle, 0)));
                trackParts[idTrackPart].transform.SetParent(Track.transform,false);
                trackParts[idTrackPart].transform.localScale = new Vector3(1, 1, gapPoints.magnitude / 10 + 1);
            }
            idTrackPart++;
        }

        while (trackParts.Count > idTrackPart)
        {
            DestroyImmediate(trackParts[idTrackPart]);
            trackParts.RemoveAt(idTrackPart);
        }
    }
}
