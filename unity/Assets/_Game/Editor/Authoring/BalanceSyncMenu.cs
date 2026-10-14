using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// Menu items that write the balance CSVs to <c>Balance~/</c> (a tilde folder, so Unity ignores
    /// it) and open the import window. See <see cref="BalanceSheet"/> for what each file holds.
    /// </summary>
    internal static class BalanceSyncMenu
    {
        private const string Root = "Tools/Hold the Hill/Authoring/Balance CSV/";

        [MenuItem(Root + "Export Towers")]
        public static void ExportTowers() => Export(BalanceSheet.Towers);

        [MenuItem(Root + "Export Enemies and Bosses")]
        public static void ExportEnemies() => Export(BalanceSheet.Enemies);

        [MenuItem(Root + "Export Power-ups")]
        public static void ExportPowerUps() => Export(BalanceSheet.PowerUps);

        [MenuItem(Root + "Export All")]
        public static void ExportAll()
        {
            foreach (BalanceSheet.Kind kind in BalanceSheet.All)
            {
                Export(kind);
            }
        }

        [MenuItem(Root + "Import (preview changes first)...")]
        public static void OpenImport() => BalanceImportWindow.Open();

        [MenuItem(Root + "Reveal Balance~ Folder")]
        public static void Reveal()
        {
            Directory.CreateDirectory(BalanceSheet.DefaultFolder);
            EditorUtility.RevealInFinder(BalanceSheet.DefaultFolder);
        }

        private static void Export(BalanceSheet.Kind kind)
        {
            string path = BalanceSheet.Export(kind, BalanceSheet.DefaultFolder);
            Debug.Log($"[Balance] Wrote {kind.Name} to {path}");
        }
    }
}
