using UnityEngine;

public class GraphicsManager : MonoBehaviour
{
    // The static instance variable that allows global access from any other script
    public static GraphicsManager Instance { get; private set; }

    // Expose your custom materials here so you can assign them in the Inspector
    [Header("Post-Processing Materials")]
    public Material FullScreenFilterMaterial;
    
    // You can add more references here in the future as your pipeline grows
    // public Material AnotherCustomMaterial;

    private void Awake()
    {
        // Standard Singleton setup: Ensure only one instance of this manager exists
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        
        // This prevents the manager from being destroyed when you load new scenes
        DontDestroyOnLoad(this.gameObject);
    }
}