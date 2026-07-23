using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu]
public class NavigationSettings : ScriptableObject{
    [SerializeField] public Material NavigationMaterial;
    [SerializeField] public Material SkillMaterial;
    [SerializeField] public GameObject NavigationMark;
    [SerializeField] public GameObject NavigationMarkEnd;
}
