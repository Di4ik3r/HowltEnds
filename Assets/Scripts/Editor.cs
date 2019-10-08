using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class TilePRNGMapGeneratorEditor : Editor
{
    //Runs when anything is changed
    public override void OnInspectorGUI()
    {
        MapGenerator map = target as MapGenerator;
        if (DrawDefaultInspector())
        {
            map.GenerateMap();
        }

        // Creature.digitalMap = map.getDigitalMap();
        // Creature.objectMap = map.getObjectMap();

    }
}
