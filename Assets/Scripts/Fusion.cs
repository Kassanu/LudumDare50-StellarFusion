using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Fusion : MonoBehaviour
{
    [SerializeField] private string[] inputMatter;
    [SerializeField] public GameObject[] outputMatter;

    [SerializeField] private float inputEnergy;

    [SerializeField] public float outputEnergy;

    [SerializeField] private int requiredTemperature;

    [SerializeField] private int outputHeat;

    public bool inputMatches(string[] matter) {
        return new HashSet<string>(inputMatter).SetEquals(matter);
    }

    public bool isDecay() {
        return this.inputMatter.Length == 1;
    }
}
