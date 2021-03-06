﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class GodController : MonoBehaviour {

    public static GodController Instance { get; private set; }

    public float lookSensitivity;
    public float moveSpeed;

    public float toolStartRadius;
    public float toolRadiusScrollMult;

    public float targetCircleFloatHeight;
    
    public GodToolAbstract[] tools;
    public GameObject[] toolUI;
    public GameObject[] placableObjects;

    public Text selectedToolText;
    public GameObject placableObjectButtonPrefab;
    public Transform placableObjectButtonPanel;

    private float xRotOffset = 0;
    [SerializeField]
    [HideInInspector]
    private Terrain terrain;
    [SerializeField]
    [HideInInspector]
    private Transform targetCircle;
    private float prevRadius = float.MinValue;

    [SerializeField]
    [HideInInspector]
    private GodToolAbstract activeTool;
    [SerializeField]
    [HideInInspector]
    private GameObject activeToolUI;
    private float toolRadius;
    private GameObject selectedObject;



    private void OnValidate()
    {
        terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        targetCircle = transform.Find("TargetCircle");
        if(toolUI == null)
        {
            toolUI = new GameObject[tools.Length];
        }
        if(toolUI.Length != tools.Length)
        {
            GameObject[] newToolUI = new GameObject[tools.Length];
            for(int i = 0; i < tools.Length && i < toolUI.Length; ++i)
            {
                newToolUI[i] = toolUI[i];
            }
            toolUI = newToolUI;
        }
        if (tools.Length > 0)
        {
            SetTool(0);
        }
    }

    private void Awake()
    {
        Instance = this;

        toolRadius = toolStartRadius;

        for(int i = 0; i < toolUI.Length; ++i)
        {
            if(toolUI[i] != null)
            {
                toolUI[i].SetActive(false);
            }
        }
        for(int i = 0; i < placableObjects.Length; ++i)
        {
            GameObject b = Instantiate(placableObjectButtonPrefab);
            Button btn = b.GetComponentInChildren<Button>();
            int idx = i;
            btn.onClick.AddListener(delegate { SetPlacableObject(idx); });
            Text btntxt = b.GetComponentInChildren<Text>();
            btntxt.text = placableObjects[i].name;
            b.transform.parent = placableObjectButtonPanel;
        }
        if(placableObjects.Length > 0)
        {
            SetPlacableObject(0);
        }
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool impact = Physics.Raycast(ray.origin, ray.direction, out hit, 10000f, LayerMask.GetMask("TerrainFloor"));
        Vector2 terrainPos = Vector2.zero;
        float tHeight = 0;
        Vector3 floorHitPos = Vector3.zero;

        if (impact)
        {
            floorHitPos = hit.point;
            Vector3 v3pos = hit.point - terrain.transform.position;
            v3pos.x /= terrain.terrainData.size.x;
            v3pos.y /= terrain.terrainData.size.y;
            v3pos.z /= terrain.terrainData.size.z;
            terrainPos = new Vector2(v3pos.x * terrain.terrainData.heightmapWidth,
                                             v3pos.z * terrain.terrainData.heightmapHeight);

            targetCircle.gameObject.SetActive(true);
            tHeight = terrain.SampleHeight(hit.point);
            targetCircle.position = new Vector3(hit.point.x, tHeight + targetCircleFloatHeight, hit.point.z);
            targetCircle.eulerAngles = Vector3.zero;
            targetCircle.localScale = new Vector3(toolRadius * 2, 1, toolRadius * 2);
            if(toolRadius != prevRadius)
            {
                prevRadius = toolRadius;
                activeTool.OnBrushRadiusChange(toolRadius);
            }
        }
        else
        {
            targetCircle.gameObject.SetActive(false);
        }

        

        impact = Physics.Raycast(ray.origin, ray.direction, out hit, 10000f, LayerMask.GetMask("Terrain"));
        Vector3 terrainHitPoint = impact ? hit.point : Vector3.zero;
        TerrainHitData data = new TerrainHitData();
        data.physicalHitPoint = terrainHitPoint;
        data.terrain = terrain;
        data.terrainHitPos = terrainPos;
        data.heightAtFloorPos = tHeight;
        data.floorHitPos = floorHitPos;
        data.floorHitPlusTHeight = new Vector3(floorHitPos.x, terrain.SampleHeight(floorHitPos), floorHitPos.z);
        float dt = Time.deltaTime;

        if (activeTool != null && !EventSystem.current.IsPointerOverGameObject())
        {

            float scrollAmt = Input.GetAxis("Mouse ScrollWheel");
            if (scrollAmt != 0f)
            {
                toolRadius += toolRadius * scrollAmt * toolRadiusScrollMult;
                activeTool.OnMouseScroll(scrollAmt, data, dt, toolRadius, selectedObject);
            }

            #region MouseButtonCalls
            if(Input.GetMouseButtonDown(0))
            {
                activeTool.OnMouseDown(0, data, dt, toolRadius, selectedObject);
            }
            else if (Input.GetMouseButton(0))
            {
                activeTool.OnMouseHeld(0, data, dt, toolRadius, selectedObject);
            }
            else if(Input.GetMouseButtonUp(0))
            {
                activeTool.OnMouseUp(0, data, dt, toolRadius, selectedObject);
            }

            if (Input.GetMouseButtonDown(1))
            {
                activeTool.OnMouseDown(1, data, dt, toolRadius, selectedObject);
            }
            else if (Input.GetMouseButton(1))
            {
                activeTool.OnMouseHeld(1, data, dt, toolRadius, selectedObject);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                activeTool.OnMouseUp(1, data, dt, toolRadius, selectedObject);
            }

            if (Input.GetMouseButtonDown(2))
            {
                activeTool.OnMouseDown(2, data, dt, toolRadius, selectedObject);
            }
            else if (Input.GetMouseButton(2))
            {
                activeTool.OnMouseHeld(2, data, dt, toolRadius, selectedObject);
            }
            else if (Input.GetMouseButtonUp(2))
            {
                activeTool.OnMouseUp(2, data, dt, toolRadius, selectedObject);
            }
            #endregion
        }

        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            transform.eulerAngles += Vector3.up * Input.GetAxis("Mouse X")
                                   * lookSensitivity;// * Time.deltaTime;
            xRotOffset += -Input.GetAxis("Mouse Y") * lookSensitivity;// * Time.deltaTime;
            xRotOffset = Mathf.Clamp(xRotOffset, -90f, 90f);
            transform.eulerAngles = new Vector3(xRotOffset, transform.eulerAngles.y, transform.eulerAngles.z);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        transform.position += transform.forward * Input.GetAxis("ForeBack") * Time.deltaTime * moveSpeed
                            + transform.right * Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed
                            + transform.up * Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        activeTool.ToolUpdate(data, dt);
    }

    public void SetTool(int idx)
    {
        if(activeToolUI != null)
        {
            activeToolUI.SetActive(false);
        }
        if(activeTool != null)
        {
            activeTool.OnToolDeselect();
        }
        activeTool = tools[idx];
        activeTool.OnToolSelect(selectedObject);
        activeTool.OnBrushRadiusChange(toolRadius);
        if(toolUI[idx] != null)
        {
            activeToolUI = toolUI[idx];
            activeToolUI.SetActive(true);
        }
        selectedToolText.text = "SELECTED:\n" + activeTool.toolName;
    }

    public void SetPlacableObject(int idx)
    {
        selectedObject = placableObjects[idx];
        activeTool.OnSlectedPlacableObjectChange(selectedObject);
    }

    //public Vector2 GetTerrainPos(Transform t)
    //{
    //    int terrainPosX = (int)(
    //        ((t.position.x - terrain.transform.position.x)
    //        / terrain.terrainData.size.x) * terrain.terrainData.heightmapWidth);
    //    int terrainPosY = (int)(
    //        ((t.position.z - terrain.transform.position.z)
    //        / terrain.terrainData.size.z) * terrain.terrainData.heightmapHeight);
    //
    //    return new Vector2(terrainPosX, terrainPosY);
    //}

}
