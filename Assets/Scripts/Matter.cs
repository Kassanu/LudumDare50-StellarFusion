using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matter : MonoBehaviour
{
    [SerializeField] public string name;
    [SerializeField] public bool canMove = true;

    public void Fuse()
    {
        DestroyImmediate(gameObject);
    }
}
