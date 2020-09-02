using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelsManager : MonoBehaviour
{
    public void GotoToIndex(int i)
    {
        SceneManager.LoadScene(i);
    }
}
