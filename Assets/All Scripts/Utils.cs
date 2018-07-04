using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is actually OUTSIDE of the Utils Class
public enum BoundsTest
{
    center,      // Is the center of the GameObject on screen?
    onScreen,    // Are the bounds entirely on screen?
    offScreen    // Are the bounds entirely off screen?
}

public class Utils : MonoBehaviour
{

    // Returns a list of all Materials on this GameObject or its children
    static public Material[] GetAllMaterials(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        List<Material> mats = new List<Material>();
        foreach (Renderer rend in rends)
        {
            mats.Add(rend.material);
        }
        return (mats.ToArray());
    }
}
