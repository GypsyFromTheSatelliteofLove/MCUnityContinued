using UnityEngine;

/// <summary>
/// PLANNED FOR MORE OPTIMIZED UNIT HANDLING
/// should manage the main game loop? so we can have one main game loop where almost all code execution stems from
/// like all updates in a frame should pass thru here (with the exception of maybe any trigger/collider checks which might be called in their respective monos)
/// </summary>

public class GameControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // INPUT - all player input gets handled - cache the inputs for later
        
        // AI - all AI calculations are handled - any attacks and movement based on attacks will be handled here

        // Movement updates - an array of Vector3[] positions will get updated with a Vector2[] directions (since this is 2D)

        // Animation updates - an array of VertsandUVs get updated based on arrays of AnimationStateData
        // we need to use the Vector2[] directions with atan2 to determine facing and forward speed (?)

        // Attack and other calculations 

        // Pass our movement Vector3[] positions to our Transform[] transforms; updates position of all units in map

        // pass our animation VertsandUV[] vertsAndUVS to Mesh[] meshes and pass to MeshFilter[] meshFilters 
        
        // Any move input cached will now be sent to an NavMeshAgent[] agents to set destination
        
        // as we iteratve thru agents, we get the desired velocity and put into Vector2[] directions array and use that as the direction for the next frame
    }

}
