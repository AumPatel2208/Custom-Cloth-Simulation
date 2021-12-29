using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Point {
    public Point(Vector3 pos) {
        position = pos;
        prevPosition = pos;
    }

    public Vector3 position, prevPosition;
    public bool isLocked;
}

public class Stick {
    public Point pointA, pointB;
    public float length;
}


public class RopeSimulation : MonoBehaviour {
    public GameObject pointPrefab;
    public GameObject stickPrefab;

    public float gravity;
    public int iterations;

    private List<Point> points;
    private List<Stick> sticks;

    public Material lockedPointMaterial;
    public Material freePointMaterial;

    private List<GameObject> pointsGameObjects;
    private List<GameObject> sticksGameObjects;


    public GameObject cursor;
    public LineRenderer stickLinePreview;
    public Stick stickInProgress;
    private bool isDrawingLine;

    public LineRenderer stickCutter;
    public bool isCutting;


    public bool toSimulate;

    public int gridWidth, gridHeight;
    public Vector3 startPosition;
    public Vector3 endPosition;

    public bool toCreateGrid;

    void Start() {
        points = new List<Point>();
        sticks = new List<Stick>();

        pointsGameObjects = new List<GameObject>();
        sticksGameObjects = new List<GameObject>();

        if (toCreateGrid)
            CreateGrid();
    }

    void Update() {
        var mousePos = Input.mousePosition;
        mousePos.z = 5.0f;
        var screenMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        if (cursor)
            cursor.transform.position = screenMousePos;


        // Add Point : Left Click
        if (Input.GetMouseButtonDown(0)) {
            AddPoint(screenMousePos);
        }

        // Delete Point : Middle Click
        if (Input.GetMouseButtonDown(2)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.CompareTag("point")) {
                    int index = pointsGameObjects.FindIndex(o => o == hit.transform.gameObject);
                    DeletePoint(index);
                }
            }
        }

        // Start New Stick : Hold Right Click
        if (Input.GetMouseButtonDown(1)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.CompareTag("point")) {
                    stickLinePreview.SetPosition(0, hit.transform.position);

                    // create stick
                    stickInProgress = new Stick();
                    int pointAIndex = pointsGameObjects.FindIndex(o => o == hit.transform.gameObject);
                    stickInProgress.pointA = points[pointAIndex];

                    isDrawingLine = true;
                }
            }
        }

        // While Right Click is held
        // Draw Stick
        if (isDrawingLine) {
            stickLinePreview.SetPosition(1, screenMousePos);
        }
        else {
            // Hide Stick
            stickLinePreview.SetPosition(0, new Vector3(1000, 1000, 999));
            stickLinePreview.SetPosition(1, new Vector3(1000, 1000, 1000));
        }

        // Set Line Release Right Click
        if (Input.GetMouseButtonUp(1)) {
            isDrawingLine = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.CompareTag("point")) {
                    // finish Create Stick
                    // Index for Point
                    int pointBIndex = pointsGameObjects.FindIndex(o => o == hit.transform.gameObject);
                    stickInProgress.pointB = points[pointBIndex];
                    stickInProgress.length =
                        Vector3.Distance(stickInProgress.pointA.position, stickInProgress.pointB.position);

                    AddStickInProgress();
                }
            }
        }

        // Cutting Line : Hold Mouse 4
        if (Input.GetMouseButtonDown(3)) {
            stickCutter.SetPosition(0, screenMousePos);
            isCutting = true;
        }

        // visualise it
        if (isCutting) {
            stickCutter.SetPosition(1, screenMousePos);
        }
        else {
            stickCutter.SetPosition(0, new Vector3(1000, 1000, 999));
            stickCutter.SetPosition(1, new Vector3(1000, 1000, 1000));
        }
        
        // Cut Line : Release Mouse 4
        if (Input.GetMouseButtonUp(3)) {
            stickCutter.SetPosition(1, screenMousePos);
            CutSticks();
            isCutting = false;
        }

        // Lock Point : Mouse 5 , L
        if (Input.GetMouseButtonDown(4) || Input.GetKeyDown(KeyCode.L)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.CompareTag("point")) {
                    int pointIndex = pointsGameObjects.FindIndex(o => o == hit.transform.gameObject);
                    Debug.Log(pointIndex);
                    LockPoint(pointIndex);
                }
            }
        }

        // Update Points Game Objects
        for (int i = 0; i < points.Count; i++) {
            pointsGameObjects[i].transform.position = points[i].position;
            if (points[i].isLocked) {
                // pointsGameObjects[i].GetComponent<Material>().color = Color.red;
                pointsGameObjects[i].GetComponent<Renderer>().material = lockedPointMaterial;
            }
            else {
                pointsGameObjects[i].GetComponent<Renderer>().material = freePointMaterial;
            }
        }

        // Update Stick Game Objects
        for (int i = 0; i < sticks.Count; i++) {
            sticksGameObjects[i].GetComponent<LineRenderer>().SetPosition(0, sticks[i].pointA.position);
            sticksGameObjects[i].GetComponent<LineRenderer>().SetPosition(1, sticks[i].pointB.position);
        }

        // Turn on Simulation : S
        if (Input.GetKeyDown(KeyCode.S)) {
            toSimulate = !toSimulate;
        }

        // Physics
        if (toSimulate)
            Simulate();

        // Debug Notes
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("________________________________");
            Debug.Log("POINTS IN: " + points.Count);
            Debug.Log("POINTS GO: " + pointsGameObjects.Count);
            Debug.Log("STICKS IN: " + sticks.Count);
            Debug.Log("STICKS GO: " + sticksGameObjects.Count);
        }
    }


    bool AlreadyExists(Stick stickToCheck) {
        bool alreadyExists = false;
        foreach (var stick in sticks) {
            alreadyExists =
                (stick.pointA == stickToCheck.pointA && stick.pointB == stickToCheck.pointB)
                ||
                (stick.pointA == stickToCheck.pointB && stick.pointB == stickToCheck.pointA);
            if (alreadyExists)
                break;
        }

        return alreadyExists;
    }

    void AddStickInProgress() {
        if (!AlreadyExists(stickInProgress)) {
            sticks.Add(stickInProgress); // add to sticks
            // create the Visualisation instance
            var stickGO = Instantiate(stickPrefab);
            stickGO.GetComponent<LineRenderer>().SetPosition(0, stickInProgress.pointA.position);
            stickGO.GetComponent<LineRenderer>().SetPosition(1, stickInProgress.pointB.position);
            sticksGameObjects.Add(stickGO);
        }
    }

    void AddStick(Point pA, Point pB) {
        Stick stick = new Stick();
        stick.pointA = pA;
        stick.pointB = pB;
        stick.length = Vector3.Distance(pA.position, pB.position);

        if (!AlreadyExists(stick)) {
            sticks.Add(stick);

            var stickGO = Instantiate(stickPrefab);
            stickGO.GetComponent<LineRenderer>().SetPosition(0, stick.pointA.position);
            stickGO.GetComponent<LineRenderer>().SetPosition(1, stick.pointB.position);
            sticksGameObjects.Add(stickGO);
        }
    }

    void DeleteStick(int index) {
        sticks.RemoveAt(index);
        Destroy(sticksGameObjects[index]);
        sticksGameObjects.RemoveAt(index);
    }

    void DeletePoint(int index) {
        // find the sticks connected to the points
        List<int> stickIndexesToRemove = new List<int>();
        for (int i = 0; i < sticks.Count; i++) {
            if (sticks[i].pointA == points[index]) {
                stickIndexesToRemove.Add(i);
            }
            else if (sticks[i].pointB == points[index]) {
                stickIndexesToRemove.Add(i);
            }
        }

        stickIndexesToRemove.Sort();
        stickIndexesToRemove.Reverse();
        // remove all connecting sticks to that point
        foreach (var stickIndex in stickIndexesToRemove.Distinct()) {
            DeleteStick(stickIndex);
        }

        points.RemoveAt(index);
        Destroy(pointsGameObjects[index].gameObject);
        pointsGameObjects.RemoveAt(index);
    }

    void AddPoint(Vector3 pos, bool isLocked = false) {
        Point newPoint = new Point(pos);
        newPoint.isLocked = isLocked;
        points.Add(newPoint);
        pointsGameObjects.Add(Instantiate(pointPrefab, newPoint.position,
            Quaternion.identity));
    }

    void LockPoint(int index) {
        points[index].isLocked = !points[index].isLocked;
    }

    void CutSticks() {
        List<int> stickIndexesToDelete = new List<int>();

        for (int i = 0; i < sticks.Count; i++) {
            if (AreLinesIntersecting(new Vector2(sticks[i].pointA.position.x, sticks[i].pointA.position.y),
                new Vector2(sticks[i].pointB.position.x, sticks[i].pointB.position.y), stickCutter.GetPosition(0),
                stickCutter.GetPosition(1), true)) {
                stickIndexesToDelete.Add(i);
            }
        }

        foreach (int i in stickIndexesToDelete) {
            DeleteStick(i);
        }
    }

    public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2,
        bool shouldIncludeEndPoints) {
        //To avoid floating point precision issues we can add a small value
        float epsilon = 0.00001f;

        bool isIntersecting = false;

        float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

        //Make sure the denominator is > 0, if not the lines are parallel
        if (denominator != 0f) {
            float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) /
                        denominator;
            float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) /
                        denominator;

            //Are the line segments intersecting if the end points are the same
            if (shouldIncludeEndPoints) {
                //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon) {
                    isIntersecting = true;
                }
            }
            else {
                //Is intersecting if u_a and u_b are between 0 and 1
                if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon) {
                    isIntersecting = true;
                }
            }
        }

        return isIntersecting;
    }

    void CreateGrid() {
        // create points
        for (int i = 0; i < gridWidth; i++) {
            float x = Vector3.Lerp(new Vector3(startPosition.x, 0f, 0f), new Vector3(endPosition.x, 0f, 0f),
                ((float) i / gridWidth)).x;

            for (int j = 0; j < gridHeight; j++) {
                float y = Vector3.Lerp(new Vector3(x, startPosition.y, 0f), new Vector3(x, endPosition.y, 0f),
                    ((float) j / gridHeight)).y;


                if (j == 0 && i % 4 == 0) {
                    AddPoint(new Vector3(x, y, startPosition.z), true);
                }
                else {
                    AddPoint(new Vector3(x, y, startPosition.z));
                }
            }
        }

        // Sticks
        for (int j = 0; j < gridWidth; j++) {
            for (int i = 0; i < gridHeight - 1; i++) {
                AddStick(points[i + j * gridHeight], points[i + 1 + j * gridHeight]);
            }
        }

        for (int j = 0; j < gridWidth - 1; j++) {
            for (int i = 0; i < gridHeight; i++) {
                AddStick(points[i + gridHeight * j], points[i + gridHeight + gridHeight * j]);
            }
        }
    }

    void Simulate() {
        foreach (var p in points) {
            if (!p.isLocked) {
                Vector3 positionBeforeUpdate = p.position;
                p.position += p.position - p.prevPosition;
                p.position += Vector3.down * gravity * Time.deltaTime * Time.deltaTime;
                p.prevPosition = positionBeforeUpdate;
            }
        }

        for (int i = 0; i < iterations; i++) {
            foreach (var stick in sticks) {
                Vector3 stickCentre = (stick.pointA.position + stick.pointB.position) / 2;
                Vector3 stickDir = (stick.pointA.position - stick.pointB.position).normalized;

                if (!stick.pointA.isLocked)
                    stick.pointA.position = stickCentre + stickDir * stick.length / 2;
                if (!stick.pointB.isLocked)
                    stick.pointB.position = stickCentre - stickDir * stick.length / 2;
            }
        }
    }
}