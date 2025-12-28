using System.Collections.Generic;

public enum ExplanationTag
{
    // Reachable?
    Height,
    Stretch,
    Occlusion,
    // Graspable?
    Size,
    Orientation,
    // Manipulatable?
    Weight,
}

public static class ExplanationMapping
{
    private static Dictionary<ExplanationTag, (string label, string detail)> _data = new()
    {
        [ExplanationTag.Height] = ("Too high to reach", "{0} is too high to reach"),
        [ExplanationTag.Stretch] = ("Too far to reach", "{0} is too far to reach"),
        [ExplanationTag.Occlusion] = ("Target placement is occluded", "{0} occluded."),
        [ExplanationTag.Size] = ("Too big to grasp", "{0} is too large to grasp."),
        [ExplanationTag.Orientation] = ("Cannot grasp from this angle", "Cannot grasp {0} from this angle."),
        [ExplanationTag.Weight] = ("Too heavy", "{0} exceeds maximum payload."),
    };
    public static string Label(ExplanationTag tag) => _data[tag].label;
    public static string Detail(ExplanationTag tag, params object[] args) => string.Format(_data[tag].detail, args);
}