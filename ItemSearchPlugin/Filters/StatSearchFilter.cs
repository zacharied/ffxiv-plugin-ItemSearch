﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace ItemSearchPlugin.Filters {
    internal class StatSearchFilter : SearchFilter {
        public class Stat {
            public BaseParam BaseParam;
            public int BaseParamIndex;
        }

        private BaseParam[] baseParams;

        private bool modeAny;


        public List<Stat> Stats = new List<Stat>();

        public StatSearchFilter(ItemSearchPluginConfig config, DataManager data) : base(config) {
            while (!data.IsDataReady) {
                Thread.Sleep(1);
            }

            Task.Run(() => {
                var baseParamCounts = new Dictionary<byte, int>();

                foreach (var p in data.GetExcelSheet<Item>().ToList().SelectMany(i => i.UnkStruct59)) {
                    if (!baseParamCounts.ContainsKey(p.BaseParam)) {
                        baseParamCounts.Add(p.BaseParam, 0);
                    }

                    baseParamCounts[p.BaseParam] += 1;
                }

                var sheet = data.GetExcelSheet<BaseParam>();
                baseParams = baseParamCounts.OrderBy(p => p.Value).Reverse().Select(pair => sheet.GetRow(pair.Key)).ToArray();
            });
        }

        public override string Name => "Has Stats";
        public override string NameLocalizationKey => "StatSearchFilter";
        public override bool IsSet => Stats.Count > 0 && Stats.Any(s => s.BaseParam != null && s.BaseParam.RowId != 0);

        public override bool CheckFilter(Item item) {
            if (baseParams == null) return true;
            if (modeAny) {
                // Match Any
                foreach (var s in Stats.Where(s => s.BaseParam != null && s.BaseParam.RowId != 0)) {
                    foreach (var p in item.UnkStruct59) {
                        if (p.BaseParam == s.BaseParam.RowId) {
                            return true;
                        }
                    }
                }

                return false;
            } else {
                // Match All

                foreach (var s in Stats.Where(s => s.BaseParam != null && s.BaseParam.RowId != 0)) {
                    bool foundMatch = false;
                    foreach (var p in item.UnkStruct59) {
                        if (p.BaseParam == s.BaseParam.RowId) {
                            foundMatch = true;
                        }
                    }

                    if (!foundMatch) {
                        return false;
                    }
                }
            }

            return true;
        }

        public override void DrawEditor() {
            var btnSize = new Vector2(24);

            if (baseParams == null) {
                // Still loading
                ImGui.Text("");
                return;
            }


            Stat doRemove = null;
            var i = 0;
            foreach (var stat in Stats) {
                if (ImGui.Button($"-###statSearchFilterRemove{i++}", btnSize)) doRemove = stat;
                ImGui.SameLine();
                var selectedParam = stat.BaseParamIndex;
                ImGui.SetNextItemWidth(200);
                if (ImGui.Combo($"###statSearchFilterSelectStat{i++}", ref selectedParam, baseParams.Select(bp => bp.RowId == 0 ? Loc.Localize("StatSearchFilterSelectStat", "Select a stat...") : bp.Name).ToArray(), baseParams.Length, 20)) {
                    stat.BaseParamIndex = selectedParam;
                    stat.BaseParam = baseParams[selectedParam];
                    Modified = true;
                }
            }

            if (doRemove != null) {
                Stats.Remove(doRemove);
                Modified = true;
            }

            if (ImGui.Button("+", btnSize)) {
                var stat = new Stat();
                Stats.Add(stat);
                Modified = true;
            }

            if (Stats.Count > 1) {
                ImGui.SameLine();
                if (ImGui.Checkbox($"{(modeAny ? Loc.Localize("StatSearchFilterMatchAny", "Match Any") : Loc.Localize("StatSearchFilterMatchAll", "Match All"))}###StatSearchFilterShowAny", ref modeAny)) {
                    Modified = true;
                }
            }
        }
    }
}
