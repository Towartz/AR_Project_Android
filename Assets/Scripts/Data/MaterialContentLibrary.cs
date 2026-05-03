using System.Collections.Generic;
using UnityEngine;

namespace ARtiGraf.Data
{
    [CreateAssetMenu(fileName = "MaterialContentLibrary", menuName = "ARtiGraf/Material Content Library")]
    public class MaterialContentLibrary : ScriptableObject
    {
        static readonly Dictionary<string, string> LegacyScanAliases = new Dictionary<string, string>
        {
            { "apple", "apel" },
            { "blendapple", "apel" },
            { "blendapplemarker", "apel" },
            { "blendcube", "jeruk" },
            { "blendcubemarker", "jeruk" },
            { "blendroundcube", "pisang" },
            { "blendroundcubemarker", "pisang" },
            { "blendcylinder", "anggur" },
            { "blendcylindermarker", "anggur" },
            { "blendpyramid", "kucing" },
            { "blendpyramidmarker", "kucing" },
            { "blendtorus", "kupukupu" },
            { "blendtorusmarker", "kupukupu" },
            { "cubes", "apel" },
            { "cube", "apel" },
            { "debugcube", "apel" }
        };

        [SerializeField] List<MaterialContentData> items = new List<MaterialContentData>();

        public IReadOnlyList<MaterialContentData> Items => items;
        public int Count => items.Count;

        public bool TryGetByReferenceImage(string referenceImageName, out MaterialContentData content)
        {
            for (int i = 0; i < items.Count; i++)
            {
                MaterialContentData item = items[i];
                if (item != null && item.ReferenceImageName == referenceImageName)
                {
                    content = item;
                    return true;
                }
            }

            content = null;
            return false;
        }

        public bool TryGetById(string id, out MaterialContentData content)
        {
            string normalizedId = MaterialContentKeyUtility.Normalize(id);
            for (int i = 0; i < items.Count; i++)
            {
                MaterialContentData item = items[i];
                if (item != null && item.NormalizedId == normalizedId)
                {
                    content = item;
                    return true;
                }
            }

            content = null;
            return false;
        }

        public bool TryGetByScanPayload(string scanPayload, out MaterialContentData content)
        {
            content = null;
            if (string.IsNullOrWhiteSpace(scanPayload))
            {
                return false;
            }

            var candidates = new List<string>();
            var seenCandidates = new HashSet<string>();
            AddCandidate(candidates, seenCandidates, scanPayload);

            int schemeIndex = scanPayload.IndexOf("://");
            if (schemeIndex >= 0 && schemeIndex + 3 < scanPayload.Length)
            {
                AddCandidate(candidates, seenCandidates, scanPayload.Substring(schemeIndex + 3));
            }

            int queryIndex = scanPayload.IndexOf('?');
            if (queryIndex >= 0 && queryIndex + 1 < scanPayload.Length)
            {
                string query = scanPayload.Substring(queryIndex + 1);
                string[] queryParts = query.Split('&');
                for (int i = 0; i < queryParts.Length; i++)
                {
                    string part = queryParts[i];
                    int equalsIndex = part.IndexOf('=');
                    AddCandidate(candidates, seenCandidates, equalsIndex >= 0 ? part.Substring(equalsIndex + 1) : part);
                }
            }

            int slashIndex = scanPayload.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex + 1 < scanPayload.Length)
            {
                AddCandidate(candidates, seenCandidates, scanPayload.Substring(slashIndex + 1));
            }

            foreach (string candidate in candidates)
            {
                if (TryMatchExactCandidate(candidate, out content))
                {
                    return true;
                }
            }

            return false;
        }

        static void AddCandidate(List<string> candidates, HashSet<string> seenCandidates, string rawValue)
        {
            string normalized = MaterialContentKeyUtility.Normalize(rawValue);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                if (LegacyScanAliases.TryGetValue(normalized, out string alias))
                {
                    AddNormalizedCandidate(candidates, seenCandidates, alias);
                }

                AddNormalizedCandidate(candidates, seenCandidates, normalized);
                AddPrefixVariants(candidates, seenCandidates, normalized);

                if (normalized.EndsWith("marker"))
                {
                    string trimmedMarker = normalized.Substring(0, normalized.Length - "marker".Length);
                    if (!string.IsNullOrWhiteSpace(trimmedMarker))
                    {
                        AddNormalizedCandidate(candidates, seenCandidates, trimmedMarker);
                        AddPrefixVariants(candidates, seenCandidates, trimmedMarker);
                    }
                }

                if (normalized == "cube" || normalized == "debugcube")
                {
                    AddNormalizedCandidate(candidates, seenCandidates, "cubes");
                }
            }
        }

        static void AddPrefixVariants(List<string> candidates, HashSet<string> seenCandidates, string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            AddKnownPrefixVariant(candidates, seenCandidates, normalized, "flashcard");
            AddKnownPrefixVariant(candidates, seenCandidates, normalized, "target");
            AddKnownPrefixVariant(candidates, seenCandidates, normalized, "marker");
            AddKnownPrefixVariant(candidates, seenCandidates, normalized, "image");

            // Vuforia targets are often named "A_Apel", "B_Buaya", etc.
            // Normalization turns those into "aapel" / "bbuaya"; strip the letter.
            if (normalized.Length > 1 && normalized[0] >= 'a' && normalized[0] <= 'z')
            {
                AddNormalizedCandidate(candidates, seenCandidates, normalized.Substring(1));
            }
        }

        static void AddKnownPrefixVariant(
            List<string> candidates,
            HashSet<string> seenCandidates,
            string normalized,
            string prefix)
        {
            if (!normalized.StartsWith(prefix) || normalized.Length <= prefix.Length)
            {
                return;
            }

            string trimmed = normalized.Substring(prefix.Length);
            AddNormalizedCandidate(candidates, seenCandidates, trimmed);
            if (trimmed.Length > 1 && trimmed[0] >= 'a' && trimmed[0] <= 'z')
            {
                AddNormalizedCandidate(candidates, seenCandidates, trimmed.Substring(1));
            }
        }

        static void AddNormalizedCandidate(List<string> candidates, HashSet<string> seenCandidates, string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized) || seenCandidates.Contains(normalized))
            {
                return;
            }

            seenCandidates.Add(normalized);
            candidates.Add(normalized);
        }

        bool TryMatchExactCandidate(string candidate, out MaterialContentData content)
        {
            for (int i = 0; i < items.Count; i++)
            {
                MaterialContentData item = items[i];
                if (item == null)
                {
                    continue;
                }

                if (item.NormalizedId == candidate ||
                    item.NormalizedReferenceImageName == candidate ||
                    item.NormalizedTitle == candidate)
                {
                    content = item;
                    return true;
                }
            }

            content = null;
            return false;
        }

        public bool Contains(MaterialContentData content)
        {
            if (content == null)
            {
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == content)
                {
                    return true;
                }
            }

            return false;
        }

        public void ReplaceItems(IReadOnlyList<MaterialContentData> replacementItems)
        {
            items.Clear();
            if (replacementItems == null)
            {
                return;
            }

            for (int i = 0; i < replacementItems.Count; i++)
            {
                MaterialContentData item = replacementItems[i];
                if (item != null)
                {
                    items.Add(item);
                }
            }
        }
    }
}
