using System;
using System.Text;
using UnityEngine;

namespace EnglishPatch.Support;
public static class ObjectHelper
{
    public static string GetPath(this object obj)
    {
        if (obj is not GameObject && obj is not Component)
        {
            throw new ArgumentException("Expected object to be a GameObject or component.", "obj");
        }

        var asset = obj is GameObject gameObject ? gameObject : ((Component)obj).gameObject;
        StringBuilder path = new StringBuilder();

        //Recurse through parents
        while (asset != null)
        {
            path.Insert(0, "/" + asset.name);
            asset = asset.transform.parent?.gameObject;
        }

        return path.ToString();
    }
}
