using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.Collection
{
    /// <summary>
    /// Satu item di grid CollectionScene.
    /// - Discovered: tampilkan thumbnail berwarna + nama + ikon bintang jika ada.
    /// - Belum discovered: tampilkan siluet abu-abu + tanda tanya.
    /// </summary>
    public class CollectionItemUI : MonoBehaviour
    {
        [SerializeField] Image thumbnailImage;
        [SerializeField] Image silhouetteOverlay;
        [SerializeField] Text itemNameText;
        [SerializeField] Text questionMarkText;
        [SerializeField] GameObject starBadge;
        [SerializeField] GameObject discoveredBadge;

        MaterialContentData linkedContent;
        bool isDiscovered;

        public void Setup(MaterialContentData content, bool discovered, bool hasStar = false)
        {
            linkedContent = content;
            isDiscovered = discovered;
            EnsureReferences();

            Image cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = discovered ? new Color(1f, 1f, 1f, 0.94f) : new Color(1f, 1f, 1f, 0f);
                cardImage.raycastTarget = discovered;
            }

            if (thumbnailImage != null)
            {
                if (discovered)
                {
                    bool hasImage = RuntimeSpriteCache.ApplyContentSprite(thumbnailImage, content, Color.white);
                    thumbnailImage.gameObject.SetActive(hasImage);
                }
                else
                {
                    thumbnailImage.sprite = null;
                    thumbnailImage.color = new Color(1f, 1f, 1f, 0f);
                    thumbnailImage.gameObject.SetActive(false);
                }
            }

            if (silhouetteOverlay != null)
            {
                silhouetteOverlay.gameObject.SetActive(false);
                silhouetteOverlay.raycastTarget = false;
            }

            if (itemNameText != null)
            {
                itemNameText.text = discovered && content != null ? content.Title : string.Empty;
                itemNameText.gameObject.SetActive(discovered);
                BuhenARTextStyle.Configure(itemNameText, 26, 16, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.06f);
            }

            if (questionMarkText != null)
            {
                questionMarkText.gameObject.SetActive(false);
                BuhenARTextStyle.Configure(questionMarkText, 58, 28, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1f);
            }

            if (starBadge != null)
                starBadge.SetActive(discovered && hasStar);

            if (discoveredBadge != null)
                discoveredBadge.SetActive(discovered);

            Button button = GetComponent<Button>();
            if (button != null)
                button.interactable = discovered;
        }

        void EnsureReferences()
        {
            if (thumbnailImage == null)
                thumbnailImage = FindChildImage("Thumbnail");
            if (silhouetteOverlay == null)
                silhouetteOverlay = FindChildImage("Silhouette");
            if (itemNameText == null)
                itemNameText = FindChildText("Name");
            if (questionMarkText == null)
                questionMarkText = FindChildText("QuestionMark");
        }

        Image FindChildImage(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i].GetComponent<Image>();
            }
            return null;
        }

        Text FindChildText(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i].GetComponent<Text>();
            }
            return null;
        }

        /// <summary>
        /// Dipanggil saat item di-tap di CollectionScene.
        /// Tampilkan detail popup jika sudah ditemukan.
        /// </summary>
        public void OnItemTapped()
        {
            if (!isDiscovered || linkedContent == null) return;
            CollectionDetailPopup popup = FindAnyObjectByType<CollectionDetailPopup>();
            if (popup != null) popup.Show(linkedContent);
        }
    }
}
