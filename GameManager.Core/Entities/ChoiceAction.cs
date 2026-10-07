using GameManager.Core.Enums;

namespace GameManager.Core.Entities;

public class ChoiceAction
{
    public ActionType Type { get; set; }
    public string Target { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsUnique { get; set; } = false;
}