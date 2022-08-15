This asset is provides:
- Shader. It blurs everything behind the element with the material of this Shader.
- Script for managing the Shader (UIBlur). It adjusts the color, intensity, and blur multiplier, has methods that gradually enable and disable blur,
and calls the events: onBeginBlur, onEndBlur, and onBlurChanged (the current intensity from 0 to 1 is passed to it).
- Example of interaction between the CanvasGroup component and UIBlur.
It shows how you can synchronize gradual blurring with the alpha of the CanvasGroup component, as well as disable and enable its blocksRaycasts.
- Ready-to-use material.
- A demo scene that shows what the blur looks like for various objects.