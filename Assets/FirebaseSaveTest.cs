using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseSaveTest : MonoBehaviour
{
    private IEnumerator Start() {
        yield return new WaitForSeconds( 1f );
        Debug.Log( "Calling test state updates" );
        Articy.Teleperformance_Test.GlobalVariables.ArticyGlobalVariables.Default.GameState.mission1_1 = true;
        Articy.Teleperformance_Test.GlobalVariables.ArticyGlobalVariables.Default.Items.A_Amaze = true;
    }
}
