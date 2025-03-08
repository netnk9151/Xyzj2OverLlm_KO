using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EnglishPatch.Support;
public static class ObjectHelper
{
    private static GameObject GetAssociatedGameObject(object obj)
    {
        if (obj is GameObject go)
        {

        }
        else if (obj is Component comp)
        {
            go = comp.gameObject;
        }
        else
        {
            throw new ArgumentException("Expected object to be a GameObject or component.", "obj");
        }

        return go;
    }

    public static string[] GetPathSegments(this object obj)
    {
        var gameObject = GetAssociatedGameObject(obj);
        var objects = new GameObject[50];

        int i = 0;
        int j = 0;

        objects[i++] = gameObject;
        while (gameObject.transform.parent != null)
        {
            gameObject = gameObject.transform.parent.gameObject;
            objects[i++] = gameObject;
        }

        var result = new string[i];
        while (--i >= 0)
        {
            result[j++] = objects[i].name;
            objects[i] = null;
        }

        return result;
    }

    public static string GetPath(this object obj)
    {
        StringBuilder path = new StringBuilder();
        var segments = GetPathSegments(obj);
        for (int i = 0; i < segments.Length; i++)
        {
            path.Append("/").Append(segments[i]);
        }

        return path.ToString();
    }
}
