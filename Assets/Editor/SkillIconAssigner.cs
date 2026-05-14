using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.EditorTools
{
    /// <summary>
    /// 60종 SkillData SO 에 Kenney rune-pack 아이콘을 자동 할당.
    /// rune-pack 보유 색상(Black/Blue/Grey) 기준 매핑:
    ///   Destruction(파괴) → Black (어두운 강렬)
    ///   Element(원소) → Blue (마법)
    ///   Control(제어) → Grey (중립)
    ///   Ultimate(궁극기) → Blue 중 후반부 인덱스로 강조
    /// 같은 카테고리 내에서 id 기반 결정적 분포 (Mathf.Abs(id) % palette.Count).
    /// </summary>
    public static class SkillIconAssigner
    {
        private const string MenuPath = "RPG Pinball/Bootstrap/Assign Kenney Skill Icons";

        private const string RuneRoot = "Assets/50. External Assets/kenny/kenney_rune-pack/PNG";

        [MenuItem(MenuPath)]
        public static void AssignAll()
        {
            var blackIcons = LoadIcons("Black");
            var blueIcons = LoadIcons("Blue");
            var greyIcons = LoadIcons("Grey");

            if (blackIcons.Count == 0 || blueIcons.Count == 0 || greyIcons.Count == 0)
            {
                Debug.LogError($"[SkillIconAssigner] kenney_rune-pack/PNG/{{Black|Blue|Grey}} 아이콘 부족 — black={blackIcons.Count} blue={blueIcons.Count} grey={greyIcons.Count}");
                return;
            }

            var skills = AssetDatabase.FindAssets("t:SkillData");
            int updated = 0, skipped = 0;
            foreach (var guid in skills)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sd = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (sd == null) { skipped++; continue; }

                List<Sprite> palette = sd.branch switch
                {
                    SkillBranch.Destruction => blackIcons,  // 물리/파괴 — 검은 룬
                    SkillBranch.Element => blueIcons,        // 원소/마법 — 파란 룬
                    SkillBranch.Control => greyIcons,        // 제어/유틸 — 회색 룬
                    _ => blueIcons
                };
                // 궁극기는 파란 룬 후반부 인덱스로 강조
                if (sd.isUltimate && blueIcons.Count > 0) palette = blueIcons;

                int idx = Mathf.Abs(sd.id) % palette.Count;
                var newIcon = palette[idx];

                var so = new SerializedObject(sd);
                var iconProp = so.FindProperty("icon");
                if (iconProp != null && iconProp.objectReferenceValue != newIcon)
                {
                    iconProp.objectReferenceValue = newIcon;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(sd);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SkillIconAssigner] {updated}건 갱신 / {skills.Length}개 SkillData / black={blackIcons.Count} blue={blueIcons.Count} grey={greyIcons.Count}");
        }

        private static List<Sprite> LoadIcons(string color)
        {
            var icons = new List<Sprite>();
            // Slab 폴더 우선 (룬 풍 느낌이 더 강함). 없으면 Rectangle 사용.
            string[] preferredSubdirs = { "Slab", "Rectangle" };
            foreach (var sub in preferredSubdirs)
            {
                string dir = Path.Combine(RuneRoot, color, sub).Replace('\\', '/');
                if (!Directory.Exists(dir)) continue;
                var guids = AssetDatabase.FindAssets("t:Sprite", new[] { dir });
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (s != null) icons.Add(s);
                }
                if (icons.Count > 0) break;
            }
            return icons;
        }
    }
}
