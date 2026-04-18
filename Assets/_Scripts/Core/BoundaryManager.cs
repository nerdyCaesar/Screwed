using UnityEngine;

public class BoundaryManager : MonoBehaviour
{
    [Header("Map Dimensions")]
    public float mapWidth = 30f;
    public float mapHeight = 20f;

    void Start()
    {
        CreateBoundary("BoundaryTop", new Vector2(0, mapHeight / 2), new Vector2(mapWidth, 0.5f));
        CreateBoundary("BoundaryBottom", new Vector2(0, -mapHeight / 2), new Vector2(mapWidth, 0.5f));
        CreateBoundary("BoundaryLeft", new Vector2(-mapWidth / 2, 0), new Vector2(0.5f, mapHeight));
        CreateBoundary("BoundaryRight", new Vector2(mapWidth / 2, 0), new Vector2(0.5f, mapHeight));
    }

    void CreateBoundary(string name, Vector2 position, Vector2 size)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.position = position;
        boundary.transform.parent = transform;

        BoxCollider2D col = boundary.AddComponent<BoxCollider2D>();
        col.size = size;
    }
}