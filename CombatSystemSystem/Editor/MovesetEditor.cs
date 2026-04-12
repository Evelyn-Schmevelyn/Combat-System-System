using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CombatSystemSystem
{
    [CustomEditor(typeof(Moveset))]
    public class MovesetEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            Moveset current = (Moveset)target;

            var container = new VisualElement();

            PropertyField fld_Moves, fld_Damage, fld_DeflectedAni, fld_ParriedAni;

            fld_Moves = SetupPropertyField("moves", "Moves");
            fld_Damage = SetupPropertyField("damage", "Weapon Damage Data");
            fld_DeflectedAni = SetupPropertyField("deflectedAnimation", "Deflected Animation");
            fld_ParriedAni = SetupPropertyField("parriedAnimation", "Parried Animation");

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
}

