﻿using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(UnityEngine.UI.LoopScrollRect), true)]
public class LoopScrollRectInspector : Editor
{
    int index = 0;
    float speed = 1000, time = 1;
    public override void OnInspectorGUI ()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        UnityEngine.UI.LoopScrollRect scroll = (UnityEngine.UI.LoopScrollRect)target;
        GUI.enabled = Application.isPlaying;

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Clear"))
        {
            scroll.ClearCells();
        }
        if (GUILayout.Button("Refresh"))
        {
            scroll.RefreshCells();
        }
        if(GUILayout.Button("Refill"))
        {
            scroll.RefillCells(scroll.totalCount);
        }
        if(GUILayout.Button("RefillFromEnd"))
        {
            scroll.RefillCellsFromEnd(scroll.totalCount);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 45;
        float w = (EditorGUIUtility.currentViewWidth - 100) / 2;
        index = EditorGUILayout.IntField("Index", index, GUILayout.Width(w));
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 60;
        speed = EditorGUILayout.FloatField("    Speed", speed, GUILayout.Width(w+15));
        if(GUILayout.Button("Scroll With Speed", GUILayout.Width(130)))
        {
            scroll.ScrollToCell(index, speed);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 60;
        time = EditorGUILayout.FloatField("    Time", time, GUILayout.Width(w+15));
        if(GUILayout.Button("Scroll Within Time", GUILayout.Width(130)))
        {
            scroll.ScrollToCellWithinTime(index, time);
        }
        EditorGUILayout.EndHorizontal();
    }
}