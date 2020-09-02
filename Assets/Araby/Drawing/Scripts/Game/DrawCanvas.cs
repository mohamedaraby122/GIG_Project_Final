using UnityEngine;
using System.Collections;



namespace IndieStudio.DrawingAndColoring.Logic
{
	[DisallowMultipleComponent]
	public class DrawCanvas : MonoBehaviour {

        ///Developed by Indie Studio
        ///https://www.assetstore.unity3d.com/en/#!/publisher/9268
        ///www.indiestd.com
        ///info@indiestd.com

        ///Developed by Indie Studio
        ///https://www.assetstore.unity3d.com/en/#!/publisher/9268
        ///www.indiestd.com
        ///info@indiestd.com
        // Use this for initialization
        public static DrawCanvas instance;

        void Awake () {
			if (instance == null) {
				instance = this;
				DontDestroyOnLoad (gameObject);
			} else {
				//Set up the render camera of the Canvas
				Canvas canvas = instance.GetComponent<Canvas> ();
				if (canvas.worldCamera == null) {
					canvas.worldCamera = Camera.main;
				}
				Destroy (gameObject);
			}
        }
       
       
    }
}
