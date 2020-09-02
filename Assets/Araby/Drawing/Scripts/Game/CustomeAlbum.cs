using System.Collections;
using System.Collections.Generic;
using UnityEngine;
///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

public class CustomeAlbum : MonoBehaviour
    {
    public static CustomeAlbum _instance;
    public static CustomeAlbum Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<CustomeAlbum>();

                if (_instance == null)
                {
                    GameObject container = new GameObject("Bicycle");
                    _instance = container.AddComponent<CustomeAlbum>();
                }
            }

            return _instance;
        }
    }
    public  Sprite[] imagesArranged;

    }

