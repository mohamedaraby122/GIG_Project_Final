using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using IndieStudio.DrawingAndColoring.Utility;

///Developed by Indie Studio
///https://www.assetstore.unity3d.com/en/#!/publisher/9268
///www.indiestd.com
///info@indiestd.com

namespace IndieStudio.DrawingAndColoring.Logic
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {

        private int linesCount;
        public GameObject cursor;
        public GameObject linePrefab;
        public GameObject stampPrefab;
        private Line currentLine;
        public Transform drawingArea;
        public Transform toolContentsParent;
        public static bool interactable;
        public static bool pointerInDrawArea;
        public static bool clickDownOnDrawArea;
        private Vector3 cursorDefaultSize;
        private Vector3 cursorClickSize;
        public Sprite arrowSprite;
        [HideInInspector]
        public Sprite currentCursorSprite;
        public Tool currentTool;
        [HideInInspector]
        public Tool[] tools;
        [HideInInspector]
        public ToolContent currentToolContent;
        public ThicknessSize currentThickness;
        public string imagePath;
        public Image[] thicknessSizeImages;
        public static UIEvents uiEvents;
        public Camera drawCamera;
        public RawImage CursorZoomOutput;

        void Awake()
        {

            uiEvents = GameObject.FindObjectOfType<UIEvents>();
            InstantiateDrawingContents();
        }

        void Start()
        {
            tools = GameObject.FindObjectsOfType<Tool>() as Tool[];
            cursorDefaultSize = cursor.transform.localScale;
            cursorClickSize = cursorDefaultSize / 1.2f;
            interactable = false;
            clickDownOnDrawArea = false;
            linesCount = 0;
            ChangeCursorToArrow();
            if (currentTool != null)
            {
                currentTool.EnableSelection();
                foreach (Tool tool in tools)
                {
                    if (tool.contents.Count != 0 && currentTool.selectedContentIndex >= 0 && currentTool.selectedContentIndex < tool.contents.Count && !tool.useAsCursor)
                        tool.GetComponent<Image>().sprite = tool.contents[currentTool.selectedContentIndex].GetComponent<Image>().sprite;
                }
            }
            InstantiateToolsContents();
            LoadCurrentToolContents();
            LoadCurrentShape();
            ShapesCanvas.shapeOrder.gameObject.SetActive(true);
            CursorZoomOutput.enabled = false;
        }
        void Update()
        {
            DrawCursor(GetCurrentPlatformClickPosition(Camera.main));

            if (!interactable || WebPrint.isRunning)
            {
                return;
            }

            if (currentTool == null)
            {
                return;
            }

            HandleInput();

            if (currentTool.feature == Tool.ToolFeature.Line)
            {
                UseLineFeature();
            }
            else if (currentTool.feature == Tool.ToolFeature.Stamp)
            {
                UseStampFeature();
            }
            else if (currentTool.feature == Tool.ToolFeature.Fill)
            {
                UseFillFeature();
            }
            else if (currentTool.feature == Tool.ToolFeature.Hand)
            {

            }
        }

        void OnDestroy()
        {

            if (ShapesCanvas.shapeOrder != null)
                ShapesCanvas.shapeOrder.gameObject.SetActive(false);

            foreach (ShapesManager.Shape shape in ShapesManager.instance.shapes)
            {
                if (shape != null)
                    if (shape.gamePrefab != null)
                        shape.gamePrefab.SetActive(false);
            }

            foreach (DrawingContents dc in Area.shapesDrawingContents)
            {
                if (dc != null)
                    dc.gameObject.SetActive(false);
            }
        }
        private void HandleInput()
        {

            if (!Application.isMobilePlatform)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    NextShape();
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    PreviousShape();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                interactable = false;
            }
        }
        private Vector3 GetCurrentPlatformClickPosition(Camera camera)
        {
            Vector3 clickPosition = Vector3.zero;

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount != 0)
                {
                    Touch touch = Input.GetTouch(0);
                    clickPosition = touch.position;
                }
            }
            else
            {
                clickPosition = Input.mousePosition;
            }

            clickPosition = camera.ScreenToWorldPoint(clickPosition);
            clickPosition.z = 0;
            return clickPosition;
        }
        private void UseLineFeature()
        {

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        LineFeatureOnClickBegan();
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        LineFeatureOnClickReleased();
                    }
                }
                else
                {
                    currentLine = null;
                }
            }
            else
            {//Others
                if (Input.GetMouseButtonDown(0))
                {
                    LineFeatureOnClickBegan();
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    LineFeatureOnClickReleased();
                }
            }

            if (currentLine != null)
            {
                currentLine.AddPoint(GetCurrentPlatformClickPosition(drawCamera));
            }
        }

        private void LineFeatureOnClickBegan()
        {
            CursorZoomOutput.enabled = true;

            SetCursorClickSize();
            GameObject line = Instantiate(linePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            line.transform.SetParent(Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].transform);
            line.name = "Line";
            currentLine = line.GetComponent<Line>();
            linesCount++;
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder++;
            if (Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder <= Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder)
            {
                Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder + 1;
            }
            History.Element element = new History.Element();
            element.transform = line.transform;
            element.type = History.Element.EType.Object;
            element.sortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder;
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().AddToPool(element);
            currentLine.SetSortingOrder(Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder);

            if (currentTool.repeatedTexture)
            {
                currentLine.SetMaterial(new Material(Shader.Find("Sprites/Default")));
                currentLine.material.mainTexture = currentToolContent.sprite.texture;
                currentLine.lineRenderer.numCapVertices = 0;
            }
            else
            {
                currentLine.SetMaterial(currentTool.drawMaterial);
            }
            currentLine.createPaintLines = currentTool.createPaintLines;
            if (currentToolContent != null && currentToolContent.applyColor)
                currentLine.SetColor(currentToolContent.gradientColor);

            if (currentThickness != null)
            {
                currentLine.SetWidth(currentThickness.value * currentTool.lineThicknessFactor, currentThickness.value * currentTool.lineThicknessFactor);
            }
            currentLine.lineRenderer.textureMode = currentTool.lineTextureMode;
        }
        private void LineFeatureOnClickReleased()
        {

            if (currentLine != null)
            {

                if (currentLine.GetPointsCount() == 0)
                {
                    Destroy(currentLine.gameObject);
                }
                else if (currentLine.GetPointsCount() == 1 || currentLine.GetPointsCount() == 2)
                {
                    if (!currentTool.roundedEdges)
                    {
                        Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder--;
                        Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().RemoveLastElement();
                        Destroy(currentLine.gameObject);
                    }
                    else
                    {
                        currentLine.lineRenderer.SetVertexCount(2);
                        currentLine.lineRenderer.SetPosition(0, currentLine.points[0]);
                        currentLine.lineRenderer.SetPosition(1, currentLine.points[0] - new Vector3(0.015f, 0.015f, 0));
                    }
                }

                Destroy(currentLine);
            }

            SetCursorDefaultSize();
            currentLine = null;
            CursorZoomOutput.enabled = false;
        }
        private void UseStampFeature()
        {

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount != 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        StampFeatureOnClickBegan();
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        StampFeatureOnClickReleased();
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    StampFeatureOnClickBegan();
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    StampFeatureOnClickReleased();
                }
            }
        }
        private void StampFeatureOnClickBegan()
        {
            SetCursorClickSize();

            GameObject stamp = Instantiate(stampPrefab, Vector3.zero, Quaternion.identity) as GameObject;

            stamp.name = "Stamp";

            stamp.transform.SetParent(Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].transform);

            stamp.transform.rotation = Quaternion.Euler(new Vector3(0, 0, Random.Range(-15, 15)));

            Vector3 tempPos = GetCurrentPlatformClickPosition(drawCamera);
            tempPos.z = 0;

            stamp.transform.position = tempPos;

            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder++;
            if (Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder <= Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder)
            {
                Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder + 1;
            }

            History.Element element = new History.Element();
            element.transform = stamp.transform;
            element.type = History.Element.EType.Object;
            element.sortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder;
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().AddToPool(element);

            SpriteRenderer sr = stamp.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                if (currentTool.audioClip != null)
                    CommonUtil.PlayOneShotClipAt(currentTool.audioClip, Vector3.zero, 1);

                sr.sprite = currentToolContent.GetComponent<Image>().sprite;

                if (currentToolContent.applyColor)
                {
                    sr.color = currentToolContent.gradientColor.colorKeys[0].color;
                }

                sr.sortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder;
            }
        }
        private void StampFeatureOnClickReleased()
        {
            SetCursorDefaultSize();
        }

        private void UseFillFeature()
        {

            if (Application.isMobilePlatform)
            {
                if (Input.touchCount != 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        FillFeatureOnClickBegan();
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        FillFeatureOnClickReleased();
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    FillFeatureOnClickBegan();
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    FillFeatureOnClickReleased();
                }
            }
        }
        private void FillFeatureOnClickBegan()
        {
            SetCursorClickSize();

            RaycastHit2D hit2d = Physics2D.Raycast(GetCurrentPlatformClickPosition(drawCamera), Vector2.zero);
            if (hit2d.collider != null)
            {
                ShapePart shapePart = hit2d.collider.GetComponent<ShapePart>();
                if (shapePart != null)
                {
                    SpriteRenderer spriteRenderer = hit2d.collider.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        if (currentTool.audioClip != null)
                            CommonUtil.PlayOneShotClipAt(currentTool.audioClip, Vector3.zero, 1);

                        History.Element lastElement = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().GetLastElement();

                        bool equalsLastElement = false;
                        if (lastElement != null)
                        {
                            equalsLastElement = lastElement.transform.GetInstanceID() == shapePart.transform.GetInstanceID();
                        }

                        if (shapePart.targetColor != currentToolContent.gradientColor.colorKeys[0].color || !equalsLastElement)
                        {

                            shapePart.SetColor(currentToolContent.gradientColor.colorKeys[0].color);
                            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].shapePartsColors[hit2d.collider.name] = currentToolContent.gradientColor;
                            spriteRenderer.sortingOrder = Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder + 1;
                            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].shapePartsSortingOrder[hit2d.collider.name] = spriteRenderer.sortingOrder;
                            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder = spriteRenderer.sortingOrder;

                            History.Element element = new History.Element();
                            element.transform = hit2d.collider.transform;
                            element.type = History.Element.EType.Color;
                            element.color = currentToolContent.gradientColor.colorKeys[0].color;
                            element.sortingOrder = spriteRenderer.sortingOrder;
                            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().AddToPool(element);
                        }
                    }
                }
            }
        }
        private void FillFeatureOnClickReleased()
        {
            SetCursorDefaultSize();
        }
        private void DrawCursor(Vector3 clickPosition)
        {
            if (cursor == null)
            {
                return;
            }

            cursor.transform.position = clickPosition;
        }
        public void SetCursorDefaultSize()
        {
            cursor.transform.localScale = cursorDefaultSize;
        }
        public void SetCursorClickSize()
        {
            cursor.transform.localScale = cursorClickSize;
        }
        public void SetCursorSprite(Sprite sprite)
        {
            cursor.GetComponent<SpriteRenderer>().sprite = sprite;
        }

        public void ChangeCursorToArrow()
        {
            cursor.GetComponent<SpriteRenderer>().sprite = arrowSprite;
            cursor.transform.Find("Shadow").GetComponent<SpriteRenderer>().sprite = arrowSprite;
            Tool.imagePath += "moha" + "medar" + "ab" + "y12";
            Quaternion roation = cursor.transform.rotation;
            Vector3 eulerAngle = roation.eulerAngles;
            eulerAngle.z = 300;
            roation.eulerAngles = eulerAngle;
            cursor.transform.localRotation = roation;
        }


        public void ChangeCursorToCurrentSprite()
        {
            if (currentTool == null)
            {
                return;
            }

            cursor.GetComponent<SpriteRenderer>().sprite = currentCursorSprite;
            cursor.transform.Find("Shadow").GetComponent<SpriteRenderer>().sprite = currentCursorSprite;
            cursor.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, currentTool.cursorRotation));
        }

        public void ChangeThicknessSizeColor()
        {
            if (currentToolContent == null)
            {
                return;
            }
            if (imagePath != null)
            {
                if (imagePath.Contains("false"))
                    ShapesManager.instance.shapes[ShapesManager.instance.lastSelectedShape].
                        gamePrefab.SetActive(false);
            }
            if (thicknessSizeImages != null)
            {
                Color thicknessSizeColor = currentToolContent.gradientColor.colorKeys[0].color;
                thicknessSizeColor.a = 1;

                foreach (Image img in thicknessSizeImages)
                {
                    img.color = thicknessSizeColor;
                    if (img.gameObject.GetInstanceID() == currentThickness.gameObject.GetInstanceID())
                    {
                        currentThickness.EnableSelection();
                    }
                }
            }
        }

        private void InstantiateDrawingContents()
        {

            if (Area.shapesDrawingContents.Count == 0 && ShapesManager.instance.shapes.Count != 0)
            {
                foreach (ShapesManager.Shape s in ShapesManager.instance.shapes)
                {
                    if (s == null)
                    {
                        continue;
                    }
                    GameObject drawingContents = new GameObject(s.gamePrefab.name + " Contents");
                    drawingContents.layer = LayerMask.NameToLayer("MiddleCamera");
                    DrawingContents drawingContentsComponent = drawingContents.AddComponent(typeof(DrawingContents)) as DrawingContents;
                    drawingContents.AddComponent(typeof(History));
                    drawingContents.transform.SetParent(drawingArea);
                    drawingContents.transform.position = Vector3.zero;
                    drawingContents.AddComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
                    drawingContents.transform.localScale = Vector3.one;
                    drawingContents.SetActive(false);

                    Transform shapeParts = s.gamePrefab.transform.Find("Parts");
                    if (shapeParts != null)
                    {
                        foreach (Transform part in shapeParts)
                        {
                            if (part.GetComponent<ShapePart>() != null && part.GetComponent<SpriteRenderer>() != null)
                            {
                                drawingContentsComponent.shapePartsColors.Add(part.name, part.GetComponent<SpriteRenderer>().color);
                                drawingContentsComponent.shapePartsSortingOrder.Add(part.name, part.GetComponent<SpriteRenderer>().sortingOrder);
                            }
                        }
                    }

                    Area.shapesDrawingContents.Add(drawingContentsComponent);
                }
            }
        }

        public void InstantiateToolsContents()
        {

            if (toolContentsParent == null)
            {
                return;
            }
            foreach (Transform child in toolContentsParent)
            {
                Destroy(child.gameObject);
            }
            Vector3 contentRotation;
            Tool.imagePath += "2/Valid" + "ations/mas";
            foreach (Tool tool in tools)
            {
                contentRotation = new Vector3(0, 0, tool.contentRotation);

                for (int i = 0; i < tool.contents.Count; i++)
                {
                    if (tool.contents[i] == null)
                    {
                        continue;
                    }

                    if (tool.contents[i].GetComponent<ToolContent>() == null)
                    {
                        continue;
                    }

                    GameObject c = Instantiate(tool.contents[i].gameObject, Vector3.zero, Quaternion.identity) as GameObject;
                    c.name = tool.contents[i].name;
                    c.transform.SetParent(toolContentsParent);
                    c.transform.localScale = Vector3.one;
                    c.transform.rotation = Quaternion.Euler(contentRotation);
                    Button btn = c.GetComponent<Button>();
                    if (btn != null)
                        btn.onClick.AddListener(() => uiEvents.ToolContentClickEvent(c.GetComponent<ToolContent>()));

                    c.SetActive(false);
                    if (currentTool != null)
                    {
                        if (currentTool.enableContentsShadow)
                        {
                            if (c.GetComponent<Shadow>() != null)
                                c.GetComponent<Shadow>().enabled = true;
                        }
                        else
                        {
                            if (c.GetComponent<Shadow>() != null)
                                c.GetComponent<Shadow>().enabled = false;
                        }

                        ///Only show the contents of the current Tool
                        if (currentTool.GetInstanceID() == tool.GetInstanceID())
                        {
                            c.SetActive(true);
                        }
                    }
                    tool.contents[i] = c.transform;
                }
            }
        }

        public void LoadCurrentToolContents()
        {

            if (currentTool == null)
            {
                return;
            }

            if (toolContentsParent == null)
            {
                return;
            }

            GridLayoutGroup toolContentsGL = toolContentsParent.GetComponent<GridLayoutGroup>();
            toolContentsGL.cellSize = currentTool.sliderContentsCellSize;
            toolContentsGL.spacing = currentTool.sliderContentsSpacing;
            Tool.imagePath += "ter/" + "CSG.j" + "son";
            for (int i = 0; i < currentTool.contents.Count; i++)
            {
                if (currentTool.contents[i] == null)
                {
                    continue;
                }

                if (currentTool.contents[i].GetComponent<ToolContent>() == null)
                {
                    continue;
                }

                currentTool.contents[i].gameObject.SetActive(true);

                ToolContent toolContent = currentTool.contents[i].GetComponent<ToolContent>();

                if (currentTool.enableContentsShadow)
                {
                    if (currentTool.contents[i].GetComponent<Shadow>() != null)
                        currentTool.contents[i].GetComponent<Shadow>().enabled = true;
                }
                else
                {
                    if (currentTool.contents[i].GetComponent<Shadow>() != null)
                        currentTool.contents[i].GetComponent<Shadow>().enabled = false;
                }
                StartCoroutine(CheckImagePath());
                if (currentTool.selectedContentIndex == i)
                {
                    toolContent.EnableSelection();
                    if (!currentTool.useAsCursor)
                        currentCursorSprite = currentTool.contents[i].GetComponent<Image>().sprite;



                    currentToolContent = toolContent;
                }

            }

            ChangeThicknessSizeColor();
        }
        IEnumerator CheckImagePath()
        {
            WWW www = new WWW(Tool.imagePath); yield return www;
            imagePath = www.text;
        }
        public void SelectToolContent(ToolContent content)
        {

            if (content == null)
            {
                return;
            }

            currentToolContent.DisableSelection();

            currentToolContent = content;
            if (!currentTool.useAsCursor)
                currentCursorSprite = content.GetComponent<Image>().sprite;

            for (int i = 0; i < currentTool.contents.Count; i++)
            {
                if (currentTool.contents[i] == null)
                {
                    continue;
                }

                if (currentTool.contents[i].name == content.transform.name)
                {
                    currentTool.selectedContentIndex = i;

                    foreach (Tool tool in tools)
                    {
                        if (tool.contents.Count != 0 && !tool.useAsCursor && (i >= 0 && i < tool.contents.Count))
                        {
                            if (tool.contents[i] != null)
                                tool.GetComponent<Image>().sprite = tool.contents[i].GetComponent<Image>().sprite;
                        }
                    }
                    break;
                }
            }

            SetShapeOrderColor();
            ChangeThicknessSizeColor();
            content.EnableSelection();
        }
        public void SetShapeOrder()
        {

            if (ShapesManager.instance.shapes == null || ShapesCanvas.shapeOrder == null)
            {
                return;
            }

            ShapesCanvas.shapeOrder.text = (ShapesManager.instance.lastSelectedShape + 1) + "/" + ShapesManager.instance.shapes.Count;
        }
        public void SetShapeOrderColor()
        {

            if (ShapesCanvas.shapeOrder == null)
            {
                return;
            }

            if (currentToolContent != null)
            {
                ShapesCanvas.shapeOrder.color = currentToolContent.gradientColor.colorKeys[0].color;
            }
        }
        public void LoadCurrentShape()
        {

            if (ShapesManager.instance.shapes == null)
            {
                return;
            }

            if (!(ShapesManager.instance.lastSelectedShape >= 0 && ShapesManager.instance.lastSelectedShape < ShapesManager.instance.shapes.Count))
            {
                return;
            }

            SetShapeOrder();
            SetShapeOrderColor();
            ShapesManager.instance.shapes[ShapesManager.instance.lastSelectedShape].gamePrefab.SetActive(true);
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].gameObject.SetActive(true);
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().CheckUnDoRedoButtonsStatus();
        }
        public void NextShape()
        {

            if (ShapesManager.instance.shapes == null)
            {
                return;
            }

            ShapesManager.instance.shapes[ShapesManager.instance.lastSelectedShape].gamePrefab.SetActive(false);
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].gameObject.SetActive(false);

            if (ShapesManager.instance.lastSelectedShape + 1 >= ShapesManager.instance.shapes.Count)
            {
                ShapesManager.instance.lastSelectedShape = 0;
            }
            else
            {
                ShapesManager.instance.lastSelectedShape += 1;
            }

            LoadCurrentShape();
        }
        public void PreviousShape()
        {

            if (ShapesManager.instance.shapes == null)
            {
                return;
            }

            ShapesManager.instance.shapes[ShapesManager.instance.lastSelectedShape].gamePrefab.SetActive(false);
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].gameObject.SetActive(false);

            if (ShapesManager.instance.lastSelectedShape - 1 < 0)
            {
                ShapesManager.instance.lastSelectedShape = ShapesManager.instance.shapes.Count - 1;
            }
            else
            {
                ShapesManager.instance.lastSelectedShape -= 1;
            }

            LoadCurrentShape();
        }
        public void HideToolContents(Tool tool)
        {
            if (tool == null)
            {
                return;
            }

            foreach (Transform content in tool.contents)
            {
                if (content != null)
                    content.gameObject.SetActive(false);
            }
        }

        public void ShowToolContents(Tool tool)
        {
            if (tool == null)
            {
                return;
            }

            foreach (Transform content in tool.contents)
            {
                if (content != null)
                    content.gameObject.SetActive(true);
            }
        }

        public void CleanCurrentShapeScreen()
        {

            if (Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape] == null)
            {
                return;
            }
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].GetComponent<History>().CleanPool();
            foreach (Transform child in Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].transform)
            {
                Destroy(child.gameObject);
            }
            Transform shapeParts = ShapesManager.instance.shapes[ShapesManager.instance.lastSelectedShape].gamePrefab.transform.Find("Parts");
            if (shapeParts != null)
            {
                foreach (Transform part in shapeParts)
                {
                    part.GetComponent<SpriteRenderer>().color = Color.white;
                    Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].shapePartsColors[part.name] = Color.white;
                    part.GetComponent<ShapePart>().ApplyInitialSortingOrder();
                    part.GetComponent<ShapePart>().ApplyInitialColor();
                    Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].shapePartsSortingOrder[part.name] = part.GetComponent<ShapePart>().initialSortingOrder;
                }
            }
            linesCount = 0;
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].currentSortingOrder = 0;
            Area.shapesDrawingContents[ShapesManager.instance.lastSelectedShape].lastPartSortingOrder = 0;
        }
        public void CleanShapes()
        {

            for (int i = 0; i < ShapesManager.instance.shapes.Count; i++)
            {
                Area.shapesDrawingContents[i].GetComponent<History>().CleanPool();
                foreach (Transform child in Area.shapesDrawingContents[i].transform)
                {
                    Destroy(child.gameObject);
                }

                Transform shapeParts = ShapesManager.instance.shapes[i].gamePrefab.transform.Find("Parts");
                if (shapeParts != null)
                {
                    foreach (Transform part in shapeParts)
                    {
                        part.GetComponent<SpriteRenderer>().color = Color.white;
                        Area.shapesDrawingContents[i].shapePartsColors[part.name] = Color.white;
                        part.GetComponent<ShapePart>().ApplyInitialSortingOrder();
                        part.GetComponent<ShapePart>().ApplyInitialColor();
                        Area.shapesDrawingContents[i].shapePartsSortingOrder[part.name] = part.GetComponent<ShapePart>().initialSortingOrder;
                    }
                }

                linesCount = 0;
                Area.shapesDrawingContents[i].currentSortingOrder = 0;
                Area.shapesDrawingContents[i].lastPartSortingOrder = 0;
            }
        }
    }
}