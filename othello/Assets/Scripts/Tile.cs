using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TokenColor { Empty, Black, White }

public class Tile : MonoBehaviour
{
    public TokenColor tokenColor = TokenColor.Empty;
    public SpriteRenderer spriteRenderer;
    public Sprite blackSprite;
    public Sprite whiteSprite;
    public Sprite[] flipToBlackFrames;
    public Sprite[] flipToWhiteFrames;
    public AudioSource audioSource;
    public AudioClip placementSound;

    public void SetToken(TokenColor color)
    {
        if (tokenColor == color) return;
        if (flipToBlackFrames == null || flipToWhiteFrames == null)
        {
            Debug.LogError("Flip animation frames are not assigned!");
            return;
        }
        if (tokenColor == TokenColor.White && color == TokenColor.Black)
            StartCoroutine(FlipAnimation(flipToBlackFrames, color));
        else if (tokenColor == TokenColor.Black && color == TokenColor.White)
            StartCoroutine(FlipAnimation(flipToWhiteFrames, color));
        else if (tokenColor == TokenColor.Empty)
        {
            tokenColor = color;
            spriteRenderer.sprite = color == TokenColor.Black ? blackSprite : whiteSprite;
            audioSource.PlayOneShot(placementSound);
        }
    }

    public bool IsEmpty()
    {
        return tokenColor == TokenColor.Empty;
    }
    private void OnMouseDown()
    {
        GameManager.Instance.OnTileClicked(this);
    }

    public bool isAnimating = false;

    IEnumerator FlipAnimation(Sprite[] frames, TokenColor newColor)
    {
        isAnimating = true;
        for (int i = 0; i < frames.Length; i++)
        {
            spriteRenderer.sprite = frames[i];
            yield return new WaitForSeconds(0.05f);
        }
        tokenColor = newColor;
        spriteRenderer.sprite = newColor == TokenColor.Black ? blackSprite : whiteSprite;
        audioSource.PlayOneShot(placementSound);
        isAnimating = false;
    }

}