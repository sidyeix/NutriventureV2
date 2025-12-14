using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RowAssignmentHelper : MonoBehaviour
{
    public int totalRows = 25;
    public float zSpacing = 5f;
    public float xSpacing = 3f;
    
    [ContextMenu("Create Grid Layout")]
    void CreateGridLayout()
    {
        // Clear existing
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
        
        // Create 73 points in a grid (25 rows × 3 columns = 75, but we need 73)
        int pointCount = 0;
        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (pointCount >= 73) break;
                
                GameObject point = new GameObject($"Row{row + 1}_{GetColumnName(col)}_{pointCount + 1}");
                point.transform.SetParent(transform);
                
                float xPos = (col - 1) * xSpacing; // -3, 0, 3
                float zPos = row * zSpacing;
                
                point.transform.localPosition = new Vector3(xPos, 0, zPos);
                
                // Add visual
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(point.transform);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = Vector3.one * 0.3f;
                DestroyImmediate(cube.GetComponent<Collider>());
                
                pointCount++;
            }
            if (pointCount >= 73) break;
        }
        
        Debug.Log($"Created {pointCount} points in grid layout");
    }
    
    string GetColumnName(int col)
    {
        return col switch
        {
            0 => "Left",
            1 => "Middle",
            2 => "Right",
            _ => "Unknown"
        };
    }
    
    [ContextMenu("Rename All Points")]
    void RenameAllPoints()
    {
        int row = 1;
        int leftCount = 0, middleCount = 0, rightCount = 0;
        
        foreach (Transform child in transform)
        {
            // Sort by position
            if (child.position.x < -1f) // Left
            {
                child.name = $"Row{row}_Left_{++leftCount}";
                if (leftCount >= 3) { row++; leftCount = 0; }
            }
            else if (child.position.x < 1f) // Middle
            {
                child.name = $"Row{row}_Middle_{++middleCount}";
                if (middleCount >= 3) { row++; middleCount = 0; }
            }
            else // Right
            {
                child.name = $"Row{row}_Right_{++rightCount}";
                if (rightCount >= 3) { row++; rightCount = 0; }
            }
        }
        
        Debug.Log("Renamed all points by row/column");
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        
        // Draw row labels
        for (int row = 0; row < totalRows; row++)
        {
            Vector3 labelPos = new Vector3(0, 0, row * zSpacing);
            Handles.Label(labelPos + Vector3.up * 2, $"Row {row + 1}", 
                new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } });
            
            // Draw row line
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(new Vector3(-xSpacing, 0, row * zSpacing), 
                           new Vector3(xSpacing, 0, row * zSpacing));
        }
    }
#endif
}