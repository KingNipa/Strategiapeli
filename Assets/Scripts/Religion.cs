using UnityEngine;

public class Religion
{
    public string Name { get; private set; }
    public Color Color { get; private set; }

    public Religion(string name, Color color)
    {
        Name = name;
        Color = color;
    }
}