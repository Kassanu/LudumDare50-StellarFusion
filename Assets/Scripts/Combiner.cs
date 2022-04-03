using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combiner : MonoBehaviour
{
    [SerializeField] private Fusion[] fusions;
    public bool canCombine(string[] matter) {
        bool found = false;
        foreach (Fusion fusion in fusions)
        {
            if (!found) {
                found = fusion.inputMatches(matter);
            }
        }

        return found;
    }

    public bool canDecay(Matter matter) {
        bool found = false;
        foreach (Fusion fusion in fusions)
        {
            if (!found) {
                found = fusion.inputMatches(new string[1]{matter.name});
            }
        }

        return found;
    }

    public GameObject[] result(string[] matter) {
        GameObject[] results = null;
        foreach (Fusion fusion in fusions)
        {
            if (fusion.inputMatches(matter) && (matter.Length > 1 || (matter.Length == 1 && fusion.isDecay()))) {
                results = fusion.outputMatter;
            }
        }

        return results;
    }

    public float resultEnergy(string[] matter) {
        float energy = 0;
        foreach (Fusion fusion in fusions)
        {
            if (fusion.inputMatches(matter) && (matter.Length > 1 || (matter.Length == 1 && fusion.isDecay()))) {
                energy = fusion.outputEnergy;
            }
        }

        return energy;
    }
}
