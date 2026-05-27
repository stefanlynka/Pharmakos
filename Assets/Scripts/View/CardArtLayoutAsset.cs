using UnityEngine;

[CreateAssetMenu(menuName = "Pharmakos/Card Art Layout", fileName = "CardArtLayout")]
public class CardArtLayoutAsset : ScriptableObject
{
    public CardArtClipRect Layout = CardArtClipRect.Full;
}
