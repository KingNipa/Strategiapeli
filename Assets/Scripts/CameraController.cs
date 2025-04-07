using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 50f;
    public float minZoom = 5f;
    public float maxZoom = 100f;
    public Vector2 panLimitMin = new Vector2(0f, 0f);
    public Vector2 panLimitMax = new Vector2(100f, 100f);
    public float zoomClampThreshold = 60f;

    // kaeran oletusasetus
    public Vector3 defaultCameraPosition = new Vector3(50f, 50f, -10f);
    private bool isDraggingUI = false;

    private Vector3 lastMouseWorldPos;

    void Update()
    {
        // Jos vuoron vaihto tai k‰sittely k‰ynniss‰, p‰ivitet‰‰n hiiren sijainti 
        if (GameManager.Instance != null &&
           (GameManager.Instance.IsTurnTransitioning || GameManager.Instance.IsProcessingTurn))
        {
            lastMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return;
        }

        if (Time.time < ignorePanningUntil)
        {
            lastMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                isDraggingUI = true;
            else
                lastMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(0))
            isDraggingUI = false;

        if (!isDraggingUI && Input.GetMouseButton(0))
            HandlePanning();

        HandleZooming();

        if (Input.GetKeyDown(KeyCode.C))
        {
            ResetCameraPosition();
            //Debug.Log("Kamera palautettu oletukseen");
        }
    }


    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Camera cam = GetComponent<Camera>();
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        }
    }

    public float panMarginPercentagex = -2.0f;
    public float panMarginPercentagey = -2.0f;
    void HandlePanning()
    {
        // Lasketaan hiiren liike maailmakoordinaateissa
        Vector3 currentMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 delta = (lastMouseWorldPos - currentMouseWorldPos) * panSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + delta;

        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.orthographicSize * cam.aspect;

        // K‰ytet‰‰n inspectorista m‰‰ritelty‰ prosenttia marginaalina
        float marginX = (panLimitMax.x - panLimitMin.x) * panMarginPercentagex;
        float marginY = (panLimitMax.y - panLimitMin.y) * panMarginPercentagey;

        // Lasketaan sallitut rajat kameran keskelle
        float minAllowedX = panLimitMin.x + marginX + halfWidth;
        float maxAllowedX = panLimitMax.x - marginX - halfWidth;
        float minAllowedY = panLimitMin.y + marginY + halfHeight;
        float maxAllowedY = panLimitMax.y - marginY - halfHeight;

        // Jos sallitut rajat menev‰t p‰‰llekk‰in, keskitet‰‰n akselilla
        if (minAllowedX > maxAllowedX)
        {
            newPos.x = (panLimitMin.x + panLimitMax.x) / 2f;
        }
        else
        {
            newPos.x = Mathf.Clamp(newPos.x, minAllowedX, maxAllowedX);
        }

        if (minAllowedY > maxAllowedY)
        {
            newPos.y = (panLimitMin.y + panLimitMax.y) / 2f;
        }
        else
        {
            newPos.y = Mathf.Clamp(newPos.y, minAllowedY, maxAllowedY);
        }

        transform.position = newPos;
        lastMouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
    }



    // kameran palauttaminen oletussijaintiin
    public void ResetCameraPosition()
    {
        transform.position = defaultCameraPosition;
    }

    void OnEnable()
    {
        lastMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    //t‰st‰ alas on vaan kokeiluja
    public void UpdateLastMouseWorldPos()
    {
        lastMouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

 
    public float ignorePanningDuration = 0.5f;  
    private float ignorePanningUntil = 0f;

    public void IgnorePanningForDuration(float duration)
    {
        ignorePanningUntil = Time.time + duration;
    }

}
