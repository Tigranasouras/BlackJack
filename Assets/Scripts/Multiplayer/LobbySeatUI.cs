using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbySeatUI : MonoBehaviour
{
    //UI
    public TMP_Text nameText;
    public Image avatarImage;
    public Button leaveButton;
    public Button inviteButton;

    //convenience
    public void SetEmpty()
    {
        if (nameText) nameText.text = "Empty";
        if (avatarImage) avatarImage.sprite = null;
        if (avatarImage) avatarImage.color = new Color(1, 1, 1, 0.2f);
        if (leaveButton) leaveButton.gameObject.SetActive(false);
        if (inviteButton) inviteButton.gameObject.SetActive(true); //Invite only on empty
    }

    public void SetOccupied(string displayName, Sprite avatar, bool isLocal)
    {
        if (nameText) nameText.text = displayName;
        if(avatarImage)
        {
            avatarImage.sprite = avatar;
            avatarImage.color = Color.white;

        }
        if (leaveButton) leaveButton.gameObject.SetActive(isLocal); //Local can leave
        if (inviteButton) inviteButton.gameObject.SetActive(!isLocal); //Show invite on empty seats; hide on occupied
    }

    
   
}
