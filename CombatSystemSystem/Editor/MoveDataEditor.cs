using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI.MessageBox;

//*
namespace CombatSystemSystem
{
    [CustomEditor(typeof(MoveData))]
    public class MoveDataEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            MoveData current = (MoveData)target;

            VisualElement container = new VisualElement();

            //Property Fields
            PropertyField fld_Input, fld_AccessibleFromNeutral, fld_CanHoldMove, fld_AnimationBase, fld_AnimationHold, fld_AnimationEnd;

            Label basicLabel = new Label("Move Type");
            basicLabel.style.fontSize = 15;
            basicLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            basicLabel.style.paddingBottom = 10;
            container.Add(basicLabel);

            fld_AccessibleFromNeutral = SetupPropertyField("accessibleFromNeutral", "Accessible From Neutral");
            fld_CanHoldMove = SetupPropertyField("canHoldMove", "Is Holdable");

            Foldout inputFoldout = new Foldout();
            inputFoldout.text = "Input Data";
            inputFoldout.style.fontSize = 15;
            inputFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            inputFoldout.style.paddingTop = 15;
            inputFoldout.style.paddingBottom = 10;
            container.Add(inputFoldout);

            fld_Input = SetupPropertyField("input", "Input", inputFoldout);

            Foldout animationFoldout = new Foldout();
            animationFoldout.text = "Animation Data";
            animationFoldout.style.fontSize = 15;
            animationFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(animationFoldout);

            if (serializedObject.FindProperty("canHoldMove").boolValue)
            {
                VisualElement aniContainer = new VisualElement();

                aniContainer.style.fontSize = 12;
                aniContainer.style.unityFontStyleAndWeight = FontStyle.Normal;
                animationFoldout.Add(aniContainer);

                fld_AnimationBase = SetupPropertyField("baseAnimation", "Start Animation Data", aniContainer);
                fld_AnimationHold = SetupPropertyField("holdAnimation", "Holding Animation Data", aniContainer);
                fld_AnimationEnd = SetupPropertyField("endAnimation", "End Animation Data", aniContainer);
            }
            else
            {
                fld_AnimationBase = SetupPropertyField("baseAnimation", "Animation Data");

                fld_AnimationBase.style.fontSize = 12;
                fld_AnimationBase.style.unityFontStyleAndWeight = FontStyle.Normal;
                animationFoldout.Add(fld_AnimationBase);
            }

            fld_CanHoldMove.RegisterValueChangeCallback((evt) => 
            {
                animationFoldout.RemoveAt(0);

                if (serializedObject.FindProperty("canHoldMove").boolValue)
                {
                    VisualElement aniContainer = new VisualElement();

                    aniContainer.style.fontSize = 12;
                    aniContainer.style.unityFontStyleAndWeight = FontStyle.Normal;
                    animationFoldout.Add(aniContainer);

                    fld_AnimationBase = SetupPropertyField("baseAnimation", "Start Animation Data", aniContainer);
                    fld_AnimationHold = SetupPropertyField("holdAnimation", "Holding Animation Data", aniContainer);
                    fld_AnimationEnd = SetupPropertyField("endAnimation", "End Animation Data", aniContainer);
                }
                else
                {
                    fld_AnimationBase = SetupPropertyField("baseAnimation", "Animation Data");

                    fld_AnimationBase.style.fontSize = 12;
                    fld_AnimationBase.style.unityFontStyleAndWeight = FontStyle.Normal;
                    animationFoldout.Add(fld_AnimationBase);
                }
            });

            return container;


            PropertyField SetupPropertyField(string propertyName, string editorLabel = null, VisualElement parent = null)
            {
                editorLabel = editorLabel ?? propertyName;

                PropertyField field = new PropertyField(serializedObject.FindProperty(propertyName), editorLabel);
                field.BindProperty(serializedObject.FindProperty(propertyName));

                if (parent == null) container.Add(field);
                else parent.Add(field);

                return field;
            }
        }
    }

    [CustomPropertyDrawer(typeof(InputData))]
    public class InputDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();
            container.style.fontSize = 12;
            container.style.unityFontStyleAndWeight = FontStyle.Normal;

            var fld_ButtonName = new TextField("Input Name");
            fld_ButtonName.BindProperty(property.FindPropertyRelative("buttonName"));

            var inputsDropdown = new DropdownField("Select Input");
            foreach (var input in InputSystem.actions)
            {
                inputsDropdown.choices.Add(input.name);
            }
            inputsDropdown.choices.Add("Custom");

            fld_ButtonName.RegisterValueChangedCallback((evt) =>
            {
                if (inputsDropdown.choices.Contains(evt.newValue)) inputsDropdown.SetValueWithoutNotify(evt.newValue);
                else inputsDropdown.SetValueWithoutNotify("Custom");
            });
            inputsDropdown.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == "Custom") fld_ButtonName.value = evt.newValue;
                else fld_ButtonName.value = evt.newValue;
            });

            var fld_MotionList = new PropertyField(property.FindPropertyRelative("motion").FindPropertyRelative("motionsList"), "Motion");
            fld_MotionList.BindProperty(property.FindPropertyRelative("motion").FindPropertyRelative("motionsList"));


            container.Add(inputsDropdown);
            container.Add(fld_ButtonName);
            container.Add(fld_MotionList);

            return container;
        }
    }

    [CustomPropertyDrawer(typeof(InputData.DirectionUnit))]
    public class DirectionUnitPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();

            TwoPaneSplitView masterPanes = new TwoPaneSplitView(0, 100, TwoPaneSplitViewOrientation.Horizontal);
            TwoPaneSplitView directionPanes = new TwoPaneSplitView(1, 70, TwoPaneSplitViewOrientation.Horizontal);

            VisualElement buttonsContainer = new VisualElement();
            ToggleButtonGroup[] directionButtonsGroups = new ToggleButtonGroup[3];

            IntegerField fld_direction = new IntegerField(1);
            fld_direction.BindProperty(property.FindPropertyRelative("direction"));

            fld_direction.RegisterValueChangedCallback((evt) => { SetDirectionValue(evt.newValue); });

            for (int i = 0; i < 3; i++)
            {
                directionButtonsGroups[i] = new ToggleButtonGroup() { allowEmptySelection = true };

                for (int j = 0; j < 3; j++)
                {
                    int buttonNumber = 6 - i * 3 + j;

                    Button button = new Button() { iconImage = Resources.Load("Directions/Dir" + (buttonNumber + 1) + "Empty", typeof(Texture2D)) as Texture2D };

                    button.style.height = 20;
                    button.style.width = 20;
                    button.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(40), new Length(40)));
                    button.style.paddingBottom = 3; button.style.paddingLeft = 3; button.style.paddingRight = 3; button.style.paddingTop = 3;

                    button.clicked += () =>
                    {
                        fld_direction.value = buttonNumber + 1;
                    };

                    directionButtonsGroups[i].Add(button);
                }

                ToggleButtonGroupState state = new ToggleButtonGroupState(0, 3);
                directionButtonsGroups[i].SetValueWithoutNotify(state);

                buttonsContainer.Add(directionButtonsGroups[i]);
            }

            directionPanes.Add(fld_direction);
            directionPanes.Add(buttonsContainer);
            SetDirectionValue(property.FindPropertyRelative("direction").intValue);


            VisualElement fieldContainer = new VisualElement();

            IntegerField fld_Duration = new IntegerField("Duration");
            fld_Duration.BindProperty(property.FindPropertyRelative("duration"));
            fieldContainer.Add(fld_Duration);

            IntegerField fld_Window = new IntegerField("Window");
            fld_Window.BindProperty(property.FindPropertyRelative("window"));
            fieldContainer.Add(fld_Window);

            Toggle fld_Strict = new Toggle("Is Strict");
            fld_Strict.BindProperty(property.FindPropertyRelative("strict"));
            fieldContainer.Add(fld_Strict);

            masterPanes.Add(directionPanes);
            masterPanes.Add(fieldContainer);

            container.Add(masterPanes);

            return container;

            void SetDirectionValue(int value)
            {
                if (value < 1 || value > 9) value = 5;

                int row = 2 - (value - 1) / 3;
                int column = (value - 1) % 3;

                ToggleButtonGroupState emptyState = new ToggleButtonGroupState(0, 3), rowState = new ToggleButtonGroupState((ulong)(1 << column), 3);
                directionButtonsGroups[0].SetValueWithoutNotify(row == 0 ? rowState : emptyState);
                directionButtonsGroups[1].SetValueWithoutNotify(row == 1 ? rowState : emptyState);
                directionButtonsGroups[2].SetValueWithoutNotify(row == 2 ? rowState : emptyState);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int buttonNumber = 6 - i * 3 + j + 1;
                        if (buttonNumber == value) (directionButtonsGroups[i].ElementAt(j) as Button).iconImage = Resources.Load("Directions/Dir" + (buttonNumber), typeof(Texture2D)) as Texture2D;
                        else (directionButtonsGroups[i].ElementAt(j) as Button).iconImage = Resources.Load("Directions/Dir" + (buttonNumber) + "Empty", typeof(Texture2D)) as Texture2D;
                    }
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(MoveData.HitboxData))]
    public class HitboxDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();

            // Check for an animation clip in parent object
            var parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf("."));
            var superParentPath = parentPath.Substring(0, parentPath.LastIndexOf("."));
            var connectedAniClipProperty = property.serializedObject.FindProperty(superParentPath).FindPropertyRelative("animation");

            // Check one more layer in case of failure
            connectedAniClipProperty = connectedAniClipProperty ?? property.serializedObject.FindProperty(superParentPath.Substring(0, superParentPath.LastIndexOf("."))).FindPropertyRelative("animation");

            if (connectedAniClipProperty == null || connectedAniClipProperty.objectReferenceValue as AnimationClip == null)
            {
                
                container = defaultEditor();

                return container;
            }

            AnimationClip connectedAniClip = connectedAniClipProperty.objectReferenceValue as AnimationClip;

            var fld_ColReferences = new PropertyField(property.FindPropertyRelative("colliderDictionaryReferences"));

            var sliderActiveRange = new MinMaxSlider(property.FindPropertyRelative("frameStart").intValue, property.FindPropertyRelative("frameEnd").intValue,
                0, Mathf.RoundToInt(connectedAniClip.length * connectedAniClip.frameRate));

            var fld_Start = new IntegerField("Start Frame: ");
            fld_Start.BindProperty(property.FindPropertyRelative("frameStart"));
            var fld_End = new IntegerField("End Frame: ");
            fld_End.BindProperty(property.FindPropertyRelative("frameEnd"));

            //Binding slider and values together
            sliderActiveRange.RegisterValueChangedCallback((evt) =>
            {
                fld_Start.value = Mathf.RoundToInt(sliderActiveRange.minValue);
                fld_End.value = Mathf.RoundToInt(sliderActiveRange.maxValue);

                sliderActiveRange.minValue = Mathf.RoundToInt(sliderActiveRange.minValue);
                sliderActiveRange.maxValue = Mathf.RoundToInt(sliderActiveRange.maxValue);
            });
            fld_Start.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.minValue = fld_Start.value;
            });
            fld_End.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.maxValue = fld_End.value;
            });

            var activeRangeTextWindow = new VisualElement();
            activeRangeTextWindow.style.flexDirection = FlexDirection.Row;

            var fld_damageData = new PropertyField(property.FindPropertyRelative("damageData"));

            var fld_grabData = new PropertyField(property.FindPropertyRelative("successfulGrabData"), "Successful Grab Data (Use only if a Grab Move)");


            container.Add(fld_ColReferences);
            container.Add(sliderActiveRange);
            activeRangeTextWindow.Add(fld_Start);
            activeRangeTextWindow.Add(fld_End);
            container.Add(activeRangeTextWindow);
            container.Add(fld_damageData);
            container.Add(fld_grabData);

            return container;
        }

        VisualElement defaultEditor()
        {
            VisualElement container = new VisualElement();

            Label warningLabel = new Label("No connected animation found");
            container.Add(warningLabel);

            return container;
        }
    }

    [CustomPropertyDrawer(typeof(MoveData.BlockingData))]
    public class BlockingDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();

            // Check for an animation clip in parent object
            var parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf("."));
            var superParentPath = parentPath.Substring(0, parentPath.LastIndexOf("."));
            var connectedAniClipProperty = property.serializedObject.FindProperty(superParentPath).FindPropertyRelative("animation");

            // Check one more layer in case of failure
            connectedAniClipProperty = connectedAniClipProperty ?? property.serializedObject.FindProperty(superParentPath.Substring(0, superParentPath.LastIndexOf("."))).FindPropertyRelative("animation");

            if (connectedAniClipProperty == null || connectedAniClipProperty.objectReferenceValue as AnimationClip == null)
            {

                container = defaultEditor();

                return container;
            }

            AnimationClip connectedAniClip = connectedAniClipProperty.objectReferenceValue as AnimationClip;

            var sliderActiveRange = new MinMaxSlider(property.FindPropertyRelative("frameStart").intValue, property.FindPropertyRelative("frameEnd").intValue,
                0, Mathf.RoundToInt(connectedAniClip.length * connectedAniClip.frameRate));

            var fld_Start = new IntegerField("Start Frame: ");
            fld_Start.BindProperty(property.FindPropertyRelative("frameStart"));
            var fld_End = new IntegerField("End Frame: ");
            fld_End.BindProperty(property.FindPropertyRelative("frameEnd"));

            //Binding slider and values together
            sliderActiveRange.RegisterValueChangedCallback((evt) =>
            {
                fld_Start.value = Mathf.RoundToInt(sliderActiveRange.minValue);
                fld_End.value = Mathf.RoundToInt(sliderActiveRange.maxValue);

                sliderActiveRange.minValue = Mathf.RoundToInt(sliderActiveRange.minValue);
                sliderActiveRange.maxValue = Mathf.RoundToInt(sliderActiveRange.maxValue);
            });
            fld_Start.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.minValue = fld_Start.value;
            });
            fld_End.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.maxValue = fld_End.value;
            });

            var activeRangeTextWindow = new VisualElement();
            activeRangeTextWindow.style.flexDirection = FlexDirection.Row;

            var fld_angle = new PropertyField(property.FindPropertyRelative("coverageAngle"), "Max Coverage Angle");

            var fld_parry = new PropertyField(property.FindPropertyRelative("isParry"));


            container.Add(sliderActiveRange);
            activeRangeTextWindow.Add(fld_Start);
            activeRangeTextWindow.Add(fld_End);
            container.Add(activeRangeTextWindow);
            container.Add(fld_angle);
            container.Add(fld_parry);

            return container;
        }

        VisualElement defaultEditor()
        {
            VisualElement container = new VisualElement();

            Label warningLabel = new Label("No connected animation found");
            container.Add(warningLabel);

            return container;
        }
    }

    [CustomPropertyDrawer(typeof(MoveData.MoveCancelData))]
    public class MoveCancelDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();

            // Check for an animation clip in parent object
            var parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf("."));
            var superParentPath = parentPath.Substring(0, parentPath.LastIndexOf("."));
            var connectedAniClipProperty = property.serializedObject.FindProperty(superParentPath).FindPropertyRelative("animation");

            // Check one more layer in case of failure
            connectedAniClipProperty = connectedAniClipProperty ?? property.serializedObject.FindProperty(superParentPath.Substring(0, superParentPath.LastIndexOf("."))).FindPropertyRelative("animation");

            if (connectedAniClipProperty == null || connectedAniClipProperty.objectReferenceValue as AnimationClip == null)
            {

                container = defaultEditor();

                return container;
            }

            AnimationClip connectedAniClip = connectedAniClipProperty.objectReferenceValue as AnimationClip;

            var sliderActiveRange = new MinMaxSlider(property.FindPropertyRelative("frameStart").intValue, property.FindPropertyRelative("frameEnd").intValue,
                0, Mathf.RoundToInt(connectedAniClip.length * connectedAniClip.frameRate));

            var fld_Start = new IntegerField("Start Frame: ");
            fld_Start.BindProperty(property.FindPropertyRelative("frameStart"));
            var fld_End = new IntegerField("End Frame: ");
            fld_End.BindProperty(property.FindPropertyRelative("frameEnd"));

            //Binding slider and values together
            sliderActiveRange.RegisterValueChangedCallback((evt) =>
            {
                fld_Start.value = Mathf.RoundToInt(sliderActiveRange.minValue);
                fld_End.value = Mathf.RoundToInt(sliderActiveRange.maxValue);

                sliderActiveRange.minValue = Mathf.RoundToInt(sliderActiveRange.minValue);
                sliderActiveRange.maxValue = Mathf.RoundToInt(sliderActiveRange.maxValue);
            });
            fld_Start.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.minValue = fld_Start.value;
            });
            fld_End.RegisterValueChangedCallback((evt) =>
            {
                sliderActiveRange.maxValue = fld_End.value;
            });

            var activeRangeTextWindow = new VisualElement();
            activeRangeTextWindow.style.flexDirection = FlexDirection.Row;

            var fld_MoveData = new PropertyField(property.FindPropertyRelative("moveData"), "Move to Cancel Into");


            container.Add(fld_MoveData);
            container.Add(sliderActiveRange);
            activeRangeTextWindow.Add(fld_Start);
            activeRangeTextWindow.Add(fld_End);
            container.Add(activeRangeTextWindow);

            return container;
        }

        VisualElement defaultEditor()
        {
            VisualElement container = new VisualElement();

            Label warningLabel = new Label("No connected animation found");
            container.Add(warningLabel);

            return container;
        }
    }

    [CustomPropertyDrawer(typeof(MoveData.HitboxData.DamageInstance))]
    public class DamageInstancePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new VisualElement();

            // Check for an animation clip in parent object
            var parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf("."));
            var superParentPath = parentPath.Substring(0, parentPath.IndexOf("."));
            var connectedAniClipProperty = property.serializedObject.FindProperty(superParentPath).FindPropertyRelative("animation");

            AnimationClip connectedAniClip = connectedAniClipProperty.objectReferenceValue as AnimationClip;

            if (connectedAniClipProperty == null || connectedAniClip == null)
            {
                container = defaultEditor();

                return container;
            }

            var timingSlider = new SliderInt(0, Mathf.RoundToInt(connectedAniClip.length * connectedAniClip.frameRate));
            var fld_Frame = new IntegerField("Frame");

            timingSlider.BindProperty(property.FindPropertyRelative("frame"));
            fld_Frame.BindProperty(property.FindPropertyRelative("frame"));

            var fld_DamageData = new PropertyField(property.FindPropertyRelative("damageData"), "Damage Data");

            container.Add(timingSlider);
            container.Add(fld_Frame);
            container.Add(fld_DamageData);

            return container;
        }

        VisualElement defaultEditor()
        {
            VisualElement container = new VisualElement();

            Label warningLabel = new Label("No connected animation found");
            container.Add(warningLabel);

            return container;
        }
    }
}
//*/