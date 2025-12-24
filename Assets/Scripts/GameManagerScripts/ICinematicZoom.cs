// ICinematicZoom removed — left as a harmless marker file for compatibility.
using System.Collections;

[System.Obsolete("ICinematicZoom removed — no zooming is performed.")]
public interface ICinematicZoom
{
    IEnumerator PlayZoom(bool toZoomed);
}