using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TesseractDemoScript : MonoBehaviour
{

    // Set the main properties
    [SerializeField] private Texture2D imageToRecognize;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private RawImage outputImage;
    private TesseractDriver _tesseractDriver;
    private string _text = "";

    private void Start()
    {
        // Set the texture for the image we want to recognize, set it to 32bit
        Texture2D texture = new Texture2D(imageToRecognize.width,
                 imageToRecognize.height, TextureFormat.ARGB32, false);
        texture.SetPixels32(imageToRecognize.GetPixels32());
        texture.Apply();

        _tesseractDriver = new TesseractDriver();
        // Recognize the Texture
        Recoginze(texture);

        // Display the image
        SetImageDisplay();
    }
    private void Recoginze(Texture2D outputTexture)
    {
        // Clear out the text
        ClearTextDisplay();

        // Add the Tesseract Version to the text to the Display 
        AddToTextDisplay(_tesseractDriver.CheckTessVersion());

        // Start up the Tesseract Driver
        _tesseractDriver.Setup();

        // Add the Recognized Text to the Display
        AddToTextDisplay(_tesseractDriver.Recognize(outputTexture));

        // Add any error messages To the Display
        AddToTextDisplay(_tesseractDriver.GetErrorMessage(), true);
    }

    // Clears the Text display
    private void ClearTextDisplay()
    {
        _text = "";
    }

    // Add text to the display -- if it's an error, console log it instead
    private void AddToTextDisplay(string text, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _text += (string.IsNullOrWhiteSpace(displayText.text) ? "" :
                  "\n") + text;

        if (isError)
            Debug.LogError(text);
        else
            Debug.Log(text);
    }

    // Called Every frame, after all the update functions have been called.
    private void LateUpdate()
    {
        displayText.text = _text;
    }

    // Create the Highlights
    private void SetImageDisplay()
    {
        RectTransform rectTransform =
             outputImage.GetComponent<RectTransform>();

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            rectTransform.rect.width *
            _tesseractDriver.GetHighlightedTexture().height /
            _tesseractDriver.GetHighlightedTexture().width);

        outputImage.texture =
            _tesseractDriver.GetHighlightedTexture();
    }

}