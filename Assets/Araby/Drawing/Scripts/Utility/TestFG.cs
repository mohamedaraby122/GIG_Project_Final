using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using  IndieStudio.DrawingAndColoring.Logic;
public class TestFG : MonoBehaviour
{
    public void Goto(int i)
    {
        SceneManager.LoadScene(i);
        if (SceneManager.GetActiveScene().buildIndex != 3)
        {
            Area.shapesDrawingContents.Clear();
            ShapesManager.instance.shapes.Clear();
            Destroy(GameObject.Find("ShapesManager"));
            Destroy(GameObject.Find("ShapesCanvas"));
            Destroy(GameObject.Find("AudioSources"));
            Destroy(GameObject.Find("AdsManager"));
            Destroy(GameObject.Find("DrawCanvas"));
        }
    }
}
