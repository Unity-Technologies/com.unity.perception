#if HDRP_PRESENT
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Perception.GroundTruth;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization.Randomizers
{
    [CustomEditor(typeof(VolumeRandomizerTag))]
    [MovedFrom("Editor.Library.Volume")]
    class VolumeRandomizerTagEditor : ParameterUIElementsEditor
    {
        VolumeRandomizerTag m_Target;
        SerializedProperty usedEffects => serializedObject.FindProperty("usedEffects");

        VisualElement m_Root;
        Button m_DeleteAllButton;
        VisualElement m_EffectOptionsContainer;
        Slider m_EffectChance;
        VisualElement m_EffectsContainer;
        VisualElement m_AvailableEffectsContainer;
        PopupField<Type> m_AvailableEffects;
        Button m_AddButton;

        void OnEnable()
        {
            m_Target = serializedObject.targetObject as VolumeRandomizerTag;
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/VolumeRandomizerTagEditor.uxml"
                ).CloneTree();

            m_EffectChance = m_Root.Q<Slider>(name = "effect_chance");
            m_DeleteAllButton = m_Root.Q<Button>(name = "delete_all_button");
            m_EffectOptionsContainer = m_Root.Q<VisualElement>(name = "effect_options_container");
            m_EffectsContainer = m_Root.Q<VisualElement>(name = "effect_container");
            m_AvailableEffectsContainer = m_Root.Q<VisualElement>(name = "available_effects_container");
            m_AddButton = m_Root.Q<Button>(name = "add_effect_button");

            if (m_Target != null)
            {
                m_EffectChance.label = $"Effect Disabled Chance ({m_Target.enableEffect.threshold * 100}%)";
                m_EffectChance.value = m_Target.enableEffect.threshold * 10;
            }
            m_EffectChance.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                var val = evt.newValue;
                m_Target.enableEffect.threshold = val / 10;
                m_EffectChance.label = $"Effect Disabled Chance ({Mathf.Round(val * 10)}%)";
            });
            m_DeleteAllButton.clicked += DeleteAllButtonClicked;
            m_AddButton.clicked += AddButtonClicked;
        }

        // (List<Type> existingTypes, List<Type> allTypes, List<Type> availableTypes)
        void UpdateAvailableEffects()
        {
            var existingTypes = m_Target.usedEffects.Select(x => x.GetType()).ToList();
            var allTypes = VolumeRandomizerTag.SupportedEffects;
            var availableTypes = allTypes.Where(t => !existingTypes.Contains(t)).ToList();

            m_AvailableEffectsContainer.Clear();
            if (availableTypes.Count > 0)
            {
                m_AvailableEffects = new PopupField<Type>(
                    availableTypes,
                    0,
                    type => $"{type.Name}",
                    type => $"{type.Name}"
                    ) { label = "Volume Effect" };
                m_AvailableEffectsContainer.Insert(0, m_AvailableEffects);
                m_EffectOptionsContainer.SetVisible(true);
            }
            else
            {
                m_EffectOptionsContainer.SetVisible(false);
            }

            //return (existingTypes.ToList(), allTypes.ToList(), availableTypes);
        }

        void UpdateUI()
        {
            m_Root.schedule.Execute(() => { CreateInspectorGUI(); }).StartingIn(10);
        }

        void AddButtonClicked()
        {
            serializedObject.Update();

            var effectsSize = usedEffects.arraySize;
            usedEffects.InsertArrayElementAtIndex(effectsSize);
            var arrayItem = usedEffects.GetArrayElementAtIndex(effectsSize);

            var effectType = m_AvailableEffects.value;
            arrayItem.managedReferenceValue = Activator.CreateInstance(effectType);

            serializedObject.ApplyModifiedProperties();
            UpdateUI();
        }

        void DeleteAllButtonClicked()
        {
            serializedObject.Update();
            usedEffects.ClearArray();
            usedEffects.arraySize = 0;

            serializedObject.ApplyModifiedProperties();
            UpdateUI();
        }

        VisualElement EffectUiFromPropertyName(SerializedProperty parentProp, SerializedProperty effectProp)
        {
            var element = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{RandomizationLibraryConfiguration.EditorUxmlDirectory}/VolumeRandomizerEffectItem.uxml"
                ).CloneTree();

            var effectLabelUi = element.Q<Label>("effect_name");
            var deleteBtnUi = element.Q<Button>("delete_btn");
            var propUi = element.Q<PropertyField>("effect_prop");

            var effectName = effectProp.managedReferenceFullTypename.Split('.').Last();
            effectName = effectName.Replace("Effect", "");
            effectLabelUi.text = $"";
            propUi.label = effectName;

            deleteBtnUi.clicked += () =>
            {
                var indexToDelete = int.Parse(Regex.Match(effectProp.propertyPath, @".+Array\.data\[(?<id>\d+)\]").Groups["id"].Value);
                parentProp.DeleteArrayElementAtIndex(indexToDelete);
                parentProp.serializedObject.ApplyModifiedProperties();
                CreateInspectorGUI();
            };

            propUi.BindProperty(effectProp);

            return element;
        }

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            UpdateAvailableEffects();
            m_EffectsContainer.Clear();
            var usedEffectsProperty = usedEffects;
            for (var i = 0; i < usedEffectsProperty.arraySize; i++)
            {
                m_EffectsContainer.Add(
                    EffectUiFromPropertyName(
                        usedEffectsProperty,
                        usedEffectsProperty.FindPropertyRelative($"Array.data[{i}]")
                    )
                );
            }

            if (usedEffectsProperty.arraySize <= 0)
            {
                m_EffectsContainer.Add(new Label("No effects added.") {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        backgroundColor = Color.black,
                        borderBottomLeftRadius = 4, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomRightRadius = 4,
                        paddingBottom = 4, paddingTop = 4, paddingLeft = 4, paddingRight = 4
                    }
                });
            }

            return m_Root;
        }
    }
}
#endif
