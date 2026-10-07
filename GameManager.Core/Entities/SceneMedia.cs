using GameManager.Core.Enums;

namespace GameManager.Core.Entities;

public class SceneMedia
{
    public MediaType Type { get; set; }
    
    // Относительный путь к файлу внутри папки assets (например: "hall.jpg")
    public string FilePath { get; set; } = string.Empty; 
}