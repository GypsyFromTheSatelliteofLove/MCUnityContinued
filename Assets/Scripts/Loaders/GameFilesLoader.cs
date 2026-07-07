using UnityEngine;
using UnityEditor;
using System.IO;
using MechCommanderUnity.Utility;

public class GameFilesLoader : MonoBehaviour
{
    public string remoteMCGPath;
    public string localMCGPath = "MCG_DATA";

    private FileManager fileManager;
    public FileManager FileManager
	{
        get
        {
            if (!ValidateMCGAssetPath(localMCGPath)) return null;

            if (fileManager == null)
            {
                fileManager = new FileManager(localMCGPath);
            }

            return fileManager;
        }
	}

    private string[] pakFilesToCopy = new string[]
    {
        "OBJECTS/OBJECT2.PAK",
        "SPRITES/LARMS90.PAK", // mech data and sprites
        "SPRITES/LEGS90.PAK",
        "SPRITES/RARMS90.PAK",
        "SPRITES/TORSOS90.PAK",
        "SPRITES/SHAPES.PAK", // contains the vehicles and objects
        "SPRITES/SHAPES90.PAK", 
        "SPRITES/SPRITES.PAK",
        "TILES/TILES.PAK", // original map tiles
        "TILES/TILES90.PAK",
        "TILES/GTILES.PAK", // expansion map tiles
        "TILES/GTILES90.PAK",
    };

    // contains the definitions of maps and missions
    private string[] fstFilesToCopy =
    {
        "MISC.FST",
        "MISSION.FST",
        "TERRAIN.FST",
    };


    public void CopyDataFiles()
	{
        if (string.IsNullOrEmpty(remoteMCGPath))
        {
            Debug.Log("no valid remote mcg path!");
            return;
        }

        if (string.IsNullOrEmpty(localMCGPath)) localMCGPath = "MCG_DATA";

        Directory.CreateDirectory(localMCGPath); // will safely create directory; no need to check safe
        Directory.CreateDirectory(Path.Combine(localMCGPath, "OBJECTS"));
        Directory.CreateDirectory(Path.Combine(localMCGPath, "SPRITES"));
        Directory.CreateDirectory(Path.Combine(localMCGPath, "TILES"));

        var remoteAssetPath = Path.Combine(remoteMCGPath, "DATA");

        for (int i = 0; i < pakFilesToCopy.Length; i++)
        {
            if (!File.Exists(Path.Combine(remoteAssetPath, pakFilesToCopy[i])))
            {
                Debug.Log("no file found at " + remoteAssetPath + "/" + pakFilesToCopy[i]);
                return;
            }
            

            File.Copy(Path.Combine(remoteAssetPath, pakFilesToCopy[i]), Path.Combine(localMCGPath, pakFilesToCopy[i]), true);

            Debug.Log("File copy success " + Path.Combine(localMCGPath, pakFilesToCopy[i]));
        }

        for (int i = 0; i < fstFilesToCopy.Length; i++)
        {
            if (!File.Exists(Path.Combine(remoteMCGPath, fstFilesToCopy[i])))
            {
                Debug.Log("no file found at " + remoteMCGPath + "/" + fstFilesToCopy[i]);
                return;
            }
            
            File.Copy(Path.Combine(remoteMCGPath, fstFilesToCopy[i]), Path.Combine(localMCGPath, fstFilesToCopy[i]), true);

            Debug.Log("File copy success " + Path.Combine(localMCGPath, fstFilesToCopy[i]));
        }
    }

    public bool ValidateMCGAssetPath(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.Log("Can´t find: " + path);
            return false;
        }

        string pathToAsset;
        for (int i = 0; i < pakFilesToCopy.Length; i++)
        {
            pathToAsset = Path.Combine(path, pakFilesToCopy[i]);
            if (!File.Exists(pathToAsset))
            {
                Debug.Log("Can´t find: " + pathToAsset);
                return false;
            }
        }

        //Debug.Log("file found " + path);
        return true;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GameFilesLoader))]
    public class GameFilesLoaderEditor : Editor
    {

        GameFilesLoader editor;

        public override void OnInspectorGUI()
        {
            editor = target as GameFilesLoader;

            // Browse for MCG path
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MCG Path", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.SelectableLabel(editor.remoteMCGPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space();

            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Locate MCG exe file", "", "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    string mcgPath = Path.GetDirectoryName(path);
                    Debug.Log("path to be checked " + path + " convert to " + mcgPath);
                    if (!editor.ValidateMCGAssetPath(mcgPath))
                    {
                        EditorUtility.DisplayDialog("Invalid Game Path", "The selected MCG path is invalid", "Close");
                    }
                    else
                    {
                        editor.remoteMCGPath = mcgPath;
                    }
                }
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Copy MCG Files")) editor.CopyDataFiles();

        }

    }
#endif
}
