using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static PlasticGui.GetProcessName;

public class TrackEditEditorWindow : EditorWindow
{
    public delegate void TrackEditorDestroyDelegate();
    TrackEditorDestroyDelegate callbackDestroy;

    GameObject Track;

    List<Vector3> dots = new List<Vector3>();
    List<Vector3> tangeants = new List<Vector3>();

    Vector3 offset = new Vector3(200, 200, 0);

    Vector3 posNewDot;

    bool isFirstLastConnected = false;

    int selectedDot = -1;
    int idDrag = -1;
    DragType dragType;

    enum DragType
    {
        DOT,
        TANGEANT,
        TANGEANT_OPPOSITE
    }

    public static TrackEditEditorWindow OpenTrackEditEditorWindow()
    {
        return GetWindow<TrackEditEditorWindow>("Edit Track");
    }

    private void OnGUI()
    {
        DotsPreviewDisplay();
        DotsEditor();
        MoveOffset();
        ApplyChanges();
    }

    public void SetDestroyCallBack(TrackEditorDestroyDelegate callback)
    {
        callbackDestroy = callback;
    }

    public void SetNewTrack(GameObject newTrack)
    {
        Track = newTrack;

        dots.Clear();
        tangeants.Clear();

        foreach (Transform child in Track.transform)
        {
             if (child.tag.Contains("dotsTrack"))
            {
                dots.Add(child.gameObject.transform.localPosition);
                tangeants.Add(child.gameObject.transform.localScale);
            }
        }

        if (dots.Count > 1 && dots[0] == dots[dots.Count - 1] && tangeants[0] == tangeants[tangeants.Count - 1])
        {
            isFirstLastConnected = true;
        }
        else
        {
            isFirstLastConnected = false;
        }


        for (int i = dots.Count; i < 2; i++)
        {
            Vector3 newGo = new Vector3();
            dots.Add(newGo);
            tangeants.Add(newGo);
        }
    }

    void DotsEditor()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.keyCode == KeyCode.Mouse0)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                Vector3 gapMouseDot = tangeants[i] + dots[i] + offset - (Vector3)Event.current.mousePosition;

                if (gapMouseDot.magnitude < 5)
                {
                    idDrag = i;
                    dragType = DragType.TANGEANT;
                    break;
                }

                gapMouseDot = -tangeants[i] + dots[i] + offset - (Vector3)Event.current.mousePosition;

                if (gapMouseDot.magnitude < 5)
                {
                    idDrag = i;
                    dragType = DragType.TANGEANT_OPPOSITE;
                    break;
                }

                gapMouseDot = dots[i] + offset - (Vector3)Event.current.mousePosition;

                if (gapMouseDot.magnitude < 5)
                {
                    idDrag = i;
                    selectedDot = i;
                    dragType = DragType.DOT;
                    break;
                }

            }
            if (idDrag == -1)
            {
                selectedDot = -1;
            }
            Focus();
        }
        if (Event.current.type == EventType.MouseDrag && idDrag != -1 && Event.current.keyCode == KeyCode.Mouse0)
        {
            if (dragType == DragType.DOT)
            {
                dots[idDrag] = (Vector3)Event.current.mousePosition - offset;
            }
            else if (dragType == DragType.TANGEANT)
            {
                tangeants[idDrag] = (Vector3)Event.current.mousePosition - (dots[idDrag] + offset);
            }
            else if (dragType == DragType.TANGEANT_OPPOSITE)
            {
                tangeants[idDrag] = -((Vector3)Event.current.mousePosition - (dots[idDrag] + offset));
            }
            if (isFirstLastConnected)
            {
                dots[dots.Count - 1] = dots[0];
                tangeants[tangeants.Count - 1] = tangeants[0];
            }
            Focus();
        }
        if (Event.current.type == EventType.MouseUp && Event.current.keyCode == KeyCode.Mouse0)
        {
            idDrag = -1;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.keyCode == KeyCode.Mouse1)
        {
            selectedDot = -1;
            for (int i = 0; i < dots.Count; i++)
            {
                Vector3 gapMouseDot = dots[i] + offset - (Vector3)Event.current.mousePosition;

                if (gapMouseDot.magnitude < 5)
                {
                    selectedDot = i;
                    break;
                }

            }

            GenericMenu menu = new GenericMenu();
            if (selectedDot != -1)
            {
                menu.AddItem(new GUIContent("Delete dot"), false, OnDeleteDotSelected);
            }
            else
            {
                posNewDot = (Vector3)Event.current.mousePosition - offset;
                menu.AddItem(new GUIContent("New dot"), false, OnNewDotSelected);
                menu.AddItem(new GUIContent("First and Last Connected"), isFirstLastConnected, OnChangeLastFirstConnected);
            }
            menu.ShowAsContext();
        }
    }

    void OnNewDotSelected()
    {
        Vector3 newDot = posNewDot;

        if (!isFirstLastConnected)
        {
            dots.Add(newDot);
            tangeants.Add(Vector3.right * 20);
        }
        else
        {
            dots.Add(dots[dots.Count - 1]);
            dots[dots.Count - 2] = newDot;
            tangeants.Add(tangeants[tangeants.Count - 1]);
            tangeants[tangeants.Count - 2] = Vector3.right * 20;
        }
    }

    void OnDeleteDotSelected()
    {
        if (dots.Count==2)
        {
            return;
        }
        dots.RemoveAt(selectedDot);
        tangeants.RemoveAt(selectedDot);
        if (selectedDot == 0 && isFirstLastConnected)
        {
            dots.RemoveAt(dots.Count - 1);
            tangeants.RemoveAt(tangeants.Count - 1);
            isFirstLastConnected = false;
        }
        selectedDot = -1;
    }

    void OnChangeLastFirstConnected()
    {
        isFirstLastConnected = !isFirstLastConnected;

        if (isFirstLastConnected)
        {
            dots.Add(dots[0]);
            tangeants.Add(tangeants[0]);
        }
        else
        {
            dots.RemoveAt(dots.Count - 1);
            tangeants.RemoveAt(tangeants.Count - 1);
        }
    }

    void DotsPreviewDisplay()
    {
        Handles.BeginGUI();

        for (int i = 1; i < dots.Count; i++)
        {
            Handles.DrawBezier(dots[i - 1] + offset, dots[i] + offset,
                (-tangeants[i - 1] * 2) + dots[i - 1] + offset, (tangeants[i] * 2) + dots[i] + offset, Color.red, null, 5);
        }

        Handles.color = new Color(138f / 256f, 43f / 256f, 226f / 256f);
        for (int i = 0; i < dots.Count; i++)
        {
            EditorGUI.DrawRect(new Rect((dots[i] + offset - new Vector3(3, 3, 0)), new Vector2(5, 5)), Color.cyan);
            if (i == selectedDot)
            {
                EditorGUI.DrawRect(new Rect((tangeants[i] + dots[i] + offset - new Vector3(3, 3, 0)), new Vector2(5, 5)), new Color(138f / 256f, 43f / 256f, 226f / 256f));
                EditorGUI.DrawRect(new Rect((-tangeants[i] + dots[i] + offset - new Vector3(3, 3, 0)), new Vector2(5, 5)), new Color(138f / 256f, 43f / 256f, 226f / 256f));
                Handles.DrawLine(tangeants[i] + dots[i] + offset, -tangeants[i] + dots[i] + offset);
            }
        }
        Handles.color = Color.white;
        Handles.EndGUI();
    }

    void MoveOffset()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
        {
            offset.y -= 10;
        }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow)
        {
            offset.y += 10;
        }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow)
        {
            offset.x -= 10;
        }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow)
        {
            offset.x += 10;
        }
        else
        {
            return;
        }

        Focus();
    }

    void ApplyChanges()
    {
        AssetDatabase.Refresh();
        int dotId = 0;
        foreach (Transform child in Track.transform)
        {
            if (child.tag.Contains("dotsTrack"))
            {
                if (dotId >= dots.Count)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    child.gameObject.transform.localPosition = dots[dotId];
                    child.gameObject.transform.localScale = tangeants[dotId];
                    dotId++;
                }
            }
        }

        for (int i = dotId; i < dots.Count; i++)
        {
            GameObject newGo = new GameObject();
            newGo.transform.SetParent(Track.transform);
            newGo.tag = "dotsTrack";

            newGo.transform.localPosition = dots[i];
            newGo.transform.localScale = tangeants[i];
        }
        EditorUtility.SetDirty(Track);
        AssetDatabase.SaveAssets();
    }
}
