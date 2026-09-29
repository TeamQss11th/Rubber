using UnityEngine;

[CreateAssetMenu(fileName = "LoadingTips", menuName = "Game/Loading Tips")]
public class LoadingTips : ScriptableObject
{
    [SerializeField, TextArea(2, 5)] private string[] tips;

    public int Count => tips == null ? 0 : tips.Length;

    public string GetTip(int index)
    {
        return index >= 0 && index < Count ? tips[index] : string.Empty;
    }
}
