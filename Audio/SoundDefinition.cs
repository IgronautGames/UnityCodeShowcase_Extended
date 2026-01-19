using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Audio
{
    [CreateAssetMenu(menuName = "Igronaut/ClumsyCrew/Defs/SoundDefinition")]
    public class SoundDefinition : SerializedScriptableObject
    {
        [EnumPaging]
        public SoundType type;
        [ValueDropdown("GetFilteredIDs")] public SoundID id;
        [ShowIf("type", SoundType.CharacterVO)] public CharacterName character;
        [ShowIf("type", SoundType.CommentatorVO)] public CommentatorType commentator;

        [ShowIf("type", SoundType.CommentatorVO)] public bool canInterruptOthers;
        [ShowIf("CanBeLooped")] public bool loop;
        [ShowIf("CanBeSpatial")] public bool spatial;
        [ShowIf("spatial")][Range(0f, 1f)] public float spatialBlend = 0f;
        [ShowIf("type", SoundType.Music)] public bool useAddressables;
        [ShowIf("useAddressables")] public string addressKey;

        [Header("Limiting")]
        [ShowIf("NeedsLimiting")] public bool useLimits;
        [ShowIf("useLimits")] public int maxInstances = 3;
        [ShowIf("useLimits")] public float cooldown = 0.05f;

        [Header("References")]
        [ShowIf("ShowNormalClips")] public List<SoundClipData> clips;
        [ShowIf("type", SoundType.CharacterVO)][OdinSerialize] public Dictionary<LanguageType, List<SoundClipData>> localizedClips = new();
        [ShowIf("useAddressables")] public AssetReferenceT<AudioClip> addressableClip;

        [Space]
        [Header("Auto Sync")]
        [SerializeField] private IdNameSyncMode syncMode = IdNameSyncMode.NameToId;
        [SerializeField] private bool syncOnlyWhenChanged = true;

        bool ShowNormalClips => type != SoundType.CharacterVO && !useAddressables;
        bool CanBeLooped => type == SoundType.Music || type == SoundType.Environment || type == SoundType.CrowdReactions;
        bool CanBeSpatial => type == SoundType.GameplayObjects || type == SoundType.ToolsWeapons || 
                            type == SoundType.CharacterFoley || type == SoundType.CharacterVO || 
                            type == SoundType.CrowdReactions || type == SoundType.Environment;

        bool NeedsLimiting => type == SoundType.GameplayObjects || type == SoundType.ToolsWeapons ||
                    type == SoundType.CharacterFoley || type == SoundType.Notifications ||
                    type == SoundType.CrowdReactions || type == SoundType.Environment;

        private IEnumerable<SoundID> GetFilteredIDs()
        {
            int group = (int)type;
            return EnumRangeUtility.FilterByGroup<SoundID>(group);
        }



        // --- internal cache to prevent loops ---
        [SerializeField, HideInInspector] private string lastName;
        [SerializeField, HideInInspector] private int lastId;
        private void OnValidate()
        {
            if (syncMode == IdNameSyncMode.Off) return;

#if UNITY_EDITOR
            if (syncOnlyWhenChanged && lastName == name && lastId == (int)id)
                return;

            if (syncMode == IdNameSyncMode.NameToId)
            {
                if (IdNameSyncUtil.TryParseEnumFromName(name, out SoundID parsed))
                    id = parsed;
            }
            else if (syncMode == IdNameSyncMode.IdToName)
            {
                IdNameSyncUtil.RenameAssetToEnum(this, id);
            }

            lastName = name;
            lastId = (int)id;
#endif
        }
    }
}

