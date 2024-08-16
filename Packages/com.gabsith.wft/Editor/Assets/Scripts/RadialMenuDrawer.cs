#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace GabSith.WFT
{
    public class RadialMenuDrawer
    {
        public static void DrawRadialMenu(Rect position, VRCExpressionsMenu expressionsMenu, bool isFirstMenu, GUIStyle labelStyle = null)
        {
            float radius = 150f;
            float innerRadius = 50f;
            float iconSize = 63f;
            float borderThickness = 3.5f;

            Color backgroundColor = new Color(0.1f, 0.255f, 0.27f, 1f);
            Color innerCircleColor = new Color(0.235f, 0.255f, 0.286f, 1f);
            Color textColor = Color.white;
            Color lineColor = new Color(0.137f, 0.404f, 0.415f, 1f);
            Color borderColor = new Color(0.137f, 0.404f, 0.415f, 1f);

            Vector2 center = new Vector2(position.x + position.width / 2, position.y + position.height / 2);

            Handles.BeginGUI();

            // Draw outer border
            Handles.color = borderColor;
            Handles.DrawSolidDisc(center, Vector3.forward, radius + borderThickness);

            // Draw background circle
            Handles.color = backgroundColor;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);

            // Draw inner border
            Handles.color = borderColor;
            Handles.DrawSolidDisc(center, Vector3.forward, innerRadius);

            // Draw inner circle
            Handles.color = innerCircleColor;
            Handles.DrawSolidDisc(center, Vector3.forward, innerRadius - borderThickness);

            List<VRCExpressionsMenu.Control> controls = expressionsMenu != null ? expressionsMenu.controls : new List<VRCExpressionsMenu.Control>();
            int itemCount = controls.Count + (isFirstMenu ? 2 : 1); // +1 for "Back", +1 for "Quick Actions" if first menu

            for (int i = 0; i < itemCount; i++)
            {
                float startAngle = (-90f - 180f / itemCount + i * (360f / itemCount)) * Mathf.Deg2Rad;
                float endAngle = (-90f - 180f / itemCount + (i + 1) * (360f / itemCount)) * Mathf.Deg2Rad;
                float midAngle = (startAngle + endAngle) / 2f;

                Vector2 itemPosition = center + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * ((radius + innerRadius) / 2f + 6f) ;

                // Draw separating lines
                Handles.color = lineColor;
                Vector2 lineStart = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * innerRadius;
                Vector2 lineEnd = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
                Handles.DrawLine(lineStart, lineEnd);

                // Prepare item name
                string itemName;
                if (i == 0) itemName = "Back";
                else if (isFirstMenu && i == 1) itemName = "Quick Actions";
                else itemName = (i - (isFirstMenu ? 2 : 1) < controls.Count) ? controls[i - (isFirstMenu ? 2 : 1)].name : "";

                // Draw item icon
                Rect iconRect = new Rect(itemPosition.x - iconSize / 2, itemPosition.y - iconSize / 2, iconSize, iconSize);
                float smallIconSize = iconSize / 1.35f;
                Rect smallIconRect = new Rect(itemPosition.x - smallIconSize / 2, itemPosition.y - smallIconSize / 2, smallIconSize, smallIconSize);
                float midIconSize = iconSize / 1.1f;
                Rect midIconRect = new Rect(itemPosition.x - midIconSize / 2, itemPosition.y - midIconSize / 2, midIconSize, midIconSize);

                if (i == 0 || i == 1 && isFirstMenu)
                {
                    if (isFirstMenu)
                    {
                        if (i == 0)
                        {
                            if (Resources.Load<Texture2D>("WFTAssets/Back Home") != null)
                                GUI.DrawTexture(midIconRect, Resources.Load<Texture2D>("WFTAssets/Back Home"), ScaleMode.ScaleToFit);
                        }
                        if (i == 1)
                        {
                            if (Resources.Load<Texture2D>("WFTAssets/Quick Actions") != null)
                                GUI.DrawTexture(smallIconRect, Resources.Load<Texture2D>("WFTAssets/Quick Actions"), ScaleMode.ScaleToFit);
                        }
                    }
                    else
                    {
                        if (i == 0)
                            GUI.DrawTexture(smallIconRect, Resources.Load<Texture2D>("WFTAssets/Back"), ScaleMode.ScaleToFit);
                    }
                }
                else if (i - (isFirstMenu ? 2 : 1) < controls.Count && controls[i - (isFirstMenu ? 2 : 1)].icon != null)
                {
                    GUI.DrawTexture(iconRect, controls[i - (isFirstMenu ? 2 : 1)].icon, ScaleMode.ScaleToFit);
                }
                else
                {
                    if (Resources.Load<Texture2D>("WFTAssets/Default Icon") != null)
                        GUI.DrawTexture(smallIconRect, Resources.Load<Texture2D>("WFTAssets/Default Icon"), ScaleMode.ScaleToFit);
                }

                // Draw item name
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.normal.textColor = textColor;
                    labelStyle.wordWrap = true;
                    labelStyle.richText = true;
                    labelStyle.fontSize = 11;
                    labelStyle.fontStyle = FontStyle.Normal;
                }


                float labelSize = 80f;
                Rect labelRect = new Rect(itemPosition.x - labelSize / 2, itemPosition.y + iconSize / 2 - labelSize / 2, labelSize, labelSize);

                //Rect labelRect = new Rect(itemPosition.x , itemPosition.y + iconSize / 2 - 15f, 85, 40);
                GUI.Label(labelRect, CommonActions.CleanRichText(itemName), labelStyle);
            }

            Handles.EndGUI();
        }
    }
}
#endif