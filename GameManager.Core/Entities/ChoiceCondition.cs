using GameManager.Core.Enums;

namespace GameManager.Core.Entities;

public class ChoiceCondition
{
    public ConditionType Type { get; set; }
    public string Target { get; set; } = string.Empty;
    public int Value { get; set; }
}